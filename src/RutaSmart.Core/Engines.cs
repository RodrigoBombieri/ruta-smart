using NetTopologySuite.Geometries;

namespace RutaSmart.Core;

public static class Geo
{
    public static double Distance(Point a, Point b)
    {
        const double rad = Math.PI / 180;
        var h = Math.Pow(Math.Sin((b.Latitude - a.Latitude) * rad / 2), 2) +
            Math.Cos(a.Latitude * rad) * Math.Cos(b.Latitude * rad) * Math.Pow(Math.Sin((b.Longitude - a.Longitude) * rad / 2), 2);
        return 6371008.8 * 2 * Math.Asin(Math.Sqrt(Math.Clamp(h, 0, 1)));
    }
    public static LineString Line(Point[] points) => new(points.Select(p => new Coordinate(p.Longitude, p.Latitude)).ToArray()) { SRID = 4326 };

    // Each edge is intersected independently to preserve repeated passages in loops.
    public static (double FromM, double ToM)[] Intervals(RouteData route, Geometry area)
    {
        var lengths = route.Geometry.Zip(route.Geometry.Skip(1), Distance).ToArray();
        var total = lengths.Sum();
        if (total <= 0) return [];
        var result = new List<(double, double)>();
        double accumulated = 0;
        for (var i = 0; i < lengths.Length; i++)
        {
            var edge = Line([route.Geometry[i], route.Geometry[i + 1]]);
            if (edge.Length > 0 && edge.EnvelopeInternal.Intersects(area.EnvelopeInternal))
            {
                Add(edge.Intersection(area));
                void Add(Geometry geometry)
                {
                    if (geometry is LineString line && line.Length > 0)
                    {
                        var a = edge.Coordinates[0]; var b = edge.Coordinates[1];
                        double Fraction(Coordinate c) => Math.Clamp(((c.X-a.X)*(b.X-a.X)+(c.Y-a.Y)*(b.Y-a.Y))/(edge.Length*edge.Length), 0, 1);
                        var fractions = line.Coordinates.Select(Fraction).ToArray();
                        result.Add(((accumulated + fractions.Min()*lengths[i])/total*route.DistanceM,
                            (accumulated + fractions.Max()*lengths[i])/total*route.DistanceM));
                    }
                    else if (geometry is GeometryCollection collection)
                        for (var j = 0; j < collection.NumGeometries; j++) Add(collection.GetGeometryN(j));
                }
            }
            accumulated += lengths[i];
        }
        return result.ToArray();
    }
}

public static class FuelEngine
{
    public static FuelResult Calculate(TripInput input, double distanceM)
    {
        var litres = (decimal)distanceM / 1000 * input.Vehicle.ConsumptionLPer100Km / 100;
        var range = Math.Max(0, input.InitialLitres - input.ReserveLitres) * 100 / input.Vehicle.ConsumptionLPer100Km;
        var purchases = Math.Max(0, litres + input.ReserveLitres - input.InitialLitres);
        return new(litres, range, purchases, litres * input.ManualPriceArsPerLitre,
            purchases * input.ManualPriceArsPerLitre, input.ManualPriceArsPerLitre.HasValue ? "user_input" : "unavailable",
            purchases > 0 ? "stations_unavailable" : "no_refuel_estimated");
    }
}

public static class TimelineEngine
{
    public static Passage At(TripInput input, RouteData route, double distanceM, bool beforePause = false)
    {
        var driving = route.DurationSeconds * distanceM / route.DistanceM;
        if(route.Legs.Length>0)
        {
            driving=0; var remaining=distanceM;
            foreach(var leg in route.Legs)
            {
                driving+=leg.DistanceM>0 ? leg.DurationSeconds*Math.Clamp(remaining/leg.DistanceM,0,1) : 0;
                remaining-=leg.DistanceM;
                if(remaining<=0) break;
            }
        }
        var pauses = input.Pauses.Where(p => beforePause ? p.DistanceM < distanceM : p.DistanceM <= distanceM).Sum(p => p.DurationSeconds);
        var at = input.DepartureAt.AddSeconds(driving + pauses);
        var uncertainty = Math.Max(900, driving * .2);
        return new(distanceM, at, at.AddSeconds(-uncertainty), at.AddSeconds(uncertainty));
    }
    public static Passage[] Build(TripInput input, RouteData route)
    {
        if (input.Pauses.Any(p => p.DistanceM > route.DistanceM))
            throw new DomainException("pause_outside_route", "Una pausa está después del destino.");
        var count = Math.Clamp((int)Math.Ceiling(route.DurationSeconds / 1800), 1, 100);
        return Enumerable.Range(0, count + 1).Select(i => route.DistanceM * i / count)
            .Concat(input.Pauses.Select(p => p.DistanceM)).Distinct().Order().Select(d => At(input, route, d)).ToArray();
    }
    public static string? Overlap(Passage entry, Passage exit, DateTimeOffset from, DateTimeOffset? to)
    {
        if (to is null) return "unknown_validity";
        if (entry.At < to && exit.At > from) return "estimated_temporal_overlap";
        return entry.Earliest < to && exit.Latest > from ? "possible_temporal_overlap" : null;
    }
}

public static class AssessmentEngine
{
    public static string Status(IEnumerable<Reason> reasons, IEnumerable<Coverage> coverage)
    {
        if (reasons.Any(r => r.Severity == "adverse")) return "adverse";
        if (reasons.Any(r => r.Severity == "caution")) return "caution";
        var list = coverage.ToArray();
        return new[] { "weather", "roads" }.All(d => list.Any(c => c.Dimension == d && c.Availability == "available"))
            ? "favorable" : "insufficient_information";
    }
    public static Assessment Build(Guid id, int version, TripInput input, RouteData route, DateTimeOffset now)
    {
        var coverage = new[] {
            new Coverage("weather", "unavailable", "Sin alertas meteorológicas aplicadas a este recorrido."),
            new Coverage("roads", "unavailable", "Sin partes viales aplicados a este recorrido."),
            new Coverage("stations", "unavailable", "Inventario de estaciones no habilitado; no hay plan de carga validado."),
            new Coverage("prices", input.ManualPriceArsPerLitre.HasValue ? "partial" : "unavailable", "Precio manual de referencia; no es un precio de surtidor verificado.") };
        Reason[] reasons = route.Synthetic ? [new("synthetic_route", "routing", "information",
            "Demostración con recorrido y tiempos sintéticos. No usar para viajar.", "demo", "1", 0, route.DistanceM, null, null, "synthetic")] : [];
        return new(id, version, "2026-10-07.1", now, now.AddMinutes(5), input, route,
            TimelineEngine.Build(input, route), FuelEngine.Calculate(input, route.DistanceM), Status(reasons, coverage), coverage, reasons);
    }
}
