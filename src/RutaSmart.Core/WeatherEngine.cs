using NetTopologySuite.Geometries;

namespace RutaSmart.Core;

public static class WeatherEngine
{
    public static WeatherMatch[] Match(TripInput input, RouteData route, IEnumerable<WeatherAlert> alerts)
    {
        if (route.Synthetic) return []; // Never attach real warnings to a fictional route.
        var matches = new List<WeatherMatch>();
        foreach (var alert in alerts.Where(a => a.MessageType != "Cancel"))
        foreach (var polygon in alert.Polygons)
        {
            var geometry = new Polygon(new LinearRing(polygon.Select(p => new Coordinate(p.Longitude,p.Latitude)).ToArray()));
            if (!geometry.EnvelopeInternal.Intersects(Geo.Line(route.Geometry).EnvelopeInternal)) continue;
            foreach (var (from,to) in Geo.Intervals(route,geometry))
            {
                var confidence = TimelineEngine.Overlap(TimelineEngine.At(input,route,from,true),TimelineEngine.At(input,route,to),alert.From,alert.To);
                if (confidence is not null) matches.Add(new(alert,confidence,from,to));
            }
        }
        // Adjacent edge intersections are consolidated, but disjoint passages remain distinct.
        var merged = new List<WeatherMatch>();
        foreach (var m in matches.OrderBy(m=>m.Alert.Id).ThenBy(m=>m.DistanceFromM))
        {
            if (merged.LastOrDefault() is { } last && last.Alert.Id==m.Alert.Id && last.Confidence==m.Confidence && m.DistanceFromM<=last.DistanceToM+1)
                merged[^1] = last with { DistanceToM=Math.Max(last.DistanceToM,m.DistanceToM) };
            else merged.Add(m);
        }
        return merged.Take(100).ToArray();
    }
    public static Assessment Apply(Assessment assessment, IEnumerable<WeatherAlert> alerts, Coverage coverage)
    {
        var matches = Match(assessment.Input,assessment.Route,alerts);
        var reasons = assessment.Reasons.Concat(matches.Select(m => new Reason("smn_cap","weather",
            m.Confidence != "unknown_validity" && m.Alert.Severity is "Extreme" or "Severe" ? "adverse" : "caution",
            $"SMN: {m.Alert.Event}. Coincidencia {m.Confidence switch { "estimated_temporal_overlap"=>"con el horario estimado", "possible_temporal_overlap"=>"solo en la ventana de incertidumbre", _=>"espacial con vigencia incierta" }}.",
            m.Alert.Url,m.Alert.Id,m.DistanceFromM,m.DistanceToM,m.Alert.From,m.Alert.To,m.Confidence))).ToArray();
        var coverages = assessment.Coverage.Select(c=>c.Dimension=="weather" ? coverage : c).ToArray();
        return assessment with { WeatherAlerts=matches,Reasons=reasons,Coverage=coverages,Status=AssessmentEngine.Status(reasons,coverages) };
    }
}
