namespace RutaSmart.Core;

public sealed record Point(double Latitude, double Longitude);
public sealed record Location(string Label, Point Point, string? GeorefId = null);
public sealed record Vehicle(string Name, string FuelType, decimal TankLitres, decimal ConsumptionLPer100Km);
public sealed record Pause(double DistanceM, int DurationSeconds);
public sealed record TripInput(Location Origin, Location Destination, DateTimeOffset DepartureAt,
    string TimeZone, Vehicle Vehicle, decimal InitialLitres, decimal ReserveLitres,
    decimal? ManualPriceArsPerLitre, Pause[] Pauses, bool Demo = false);
public sealed record RouteData(string Provider, bool Synthetic, Point[] Geometry, double DistanceM,
    double DurationSeconds, string Attribution, string[] Limitations)
{
    public RouteRoad[] Roads { get; init; } = [];
    public bool CrossesInternationalBorder { get; init; }
    public RouteLeg[] Legs { get; init; } = [];
}
public sealed record RouteLeg(double DistanceM, double DurationSeconds);
public sealed record FuelPlanRequest(int ExpectedVersion, bool Accept);
public sealed record FuelStop(FuelStation Station, double DistanceM, decimal ArrivalLitres,
    decimal PurchaseLitres, decimal DepartureLitres, DateTimeOffset ArrivalAt, int PauseSeconds);
public sealed record FuelPlan(string Status, string Message, DateTimeOffset ComputedAt, RouteData? Route,
    FuelStop[] Stops, double ExtraDistanceM, double ExtraSeconds, decimal PurchaseLitres);
public sealed record RouteRoad(string Reference, string Province, double DistanceFromM, double DistanceToM);
public sealed record RoadNotice(string Id, string Province, string Road, string Section, string Status,
    string Surface, string Description, DateTimeOffset? UpdatedAt, string Url);
public sealed record RoadMatch(RoadNotice Notice, string Confidence, double DistanceFromM, double DistanceToM, bool Stale);
public sealed record Passage(double DistanceM, DateTimeOffset At, DateTimeOffset Earliest, DateTimeOffset Latest);
public sealed record FuelResult(decimal ConsumedLitres, decimal InitialRangeKm, decimal MinimumPurchaseLitres,
    decimal? ConsumptionCostArs, decimal? MinimumPurchaseCostArs, string PriceOrigin, string PlanStatus);
public sealed record Coverage(string Dimension, string Availability, string Message, DateTimeOffset? UpdatedAt = null);
public sealed record Reason(string Code, string Category, string Severity, string Message, string SourceRef,
    string EvidenceVersion, double DistanceFromM, double DistanceToM, DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidTo, string Confidence);
public sealed record Assessment(Guid TripId, int Version, string RuleVersion, DateTimeOffset ComputedAt,
    DateTimeOffset ValidUntil, TripInput Input, RouteData Route, Passage[] Timeline, FuelResult Fuel,
    string Status, Coverage[] Coverage, Reason[] Reasons)
{
    public WeatherMatch[] WeatherAlerts { get; init; } = [];
    public StationCandidate[] Stations { get; init; } = [];
    public RoadMatch[] RoadNotices { get; init; } = [];
    public FuelPlan? FuelPlan { get; init; }
}
public sealed record StationPrice(decimal AmountArsPerLitre, DateTimeOffset DeclaredAt, string Schedule);
public sealed record FuelStation(string Id, string Name, string Brand, string Address, Point Point,
    string FuelType, StationPrice[] Prices);
public sealed record StationCandidate(FuelStation Station, double DistanceAlongM, double LateralDistanceM,
    string AccessStatus, bool WithinInitialRangeWithoutDetour);
public sealed record WeatherAlert(string Id, string Sender, DateTimeOffset Sent, string MessageType,
    string[] References, string Event, string Severity, string Description, string Instruction,
    DateTimeOffset From, DateTimeOffset? To, string Url, Point[][] Polygons);
public sealed record WeatherMatch(WeatherAlert Alert, string Confidence, double DistanceFromM, double DistanceToM);
public sealed class DomainException(string code, string message, int status = 422) : Exception(message)
{
    public string Code { get; } = code;
    public int Status { get; } = status;
}

public static class InputValidator
{
    public static void Validate(TripInput input, DateTimeOffset now)
    {
        if (input.Origin?.Point is null || input.Destination?.Point is null || input.Vehicle is null || input.Pauses is null)
            throw new DomainException("invalid_input", "Completá los puntos, el vehículo y las pausas.");
        foreach (var location in new[] { input.Origin, input.Destination })
        {
            var p = location.Point;
            if (string.IsNullOrWhiteSpace(location.Label) || location.Label.Length > 120 ||
                !double.IsFinite(p.Latitude) || !double.IsFinite(p.Longitude) ||
                p.Latitude is < -55.2 or > -21.7 || p.Longitude is < -73.6 or > -53.5)
                throw new DomainException("unsupported_location", "Confirmá un punto dentro del área argentina de planificación.");
        }
        if (Geo.Distance(input.Origin.Point, input.Destination.Point) < 100)
            throw new DomainException("same_origin_destination", "Origen y destino deben ser diferentes (al menos 100 metros).");
        if (input.DepartureAt < now.AddMinutes(-1) || input.DepartureAt > now.AddDays(30))
            throw new DomainException("invalid_departure", "La salida debe estar dentro de los próximos 30 días.");
        TimeZoneInfo zone;
        try { zone = TimeZoneInfo.FindSystemTimeZoneById(input.TimeZone); }
        catch (Exception e) when (e is TimeZoneNotFoundException or InvalidTimeZoneException or ArgumentException)
        { throw new DomainException("invalid_timezone", "La zona horaria no es válida."); }
        if (!input.TimeZone.StartsWith("America/Argentina/", StringComparison.Ordinal) ||
            zone.GetUtcOffset(input.DepartureAt) != input.DepartureAt.Offset)
            throw new DomainException("invalid_offset", "La fecha debe incluir el offset de la zona argentina indicada.");
        var v = input.Vehicle;
        if (string.IsNullOrWhiteSpace(v.Name) || v.Name.Length > 80 ||
            v.FuelType is not ("nafta_2" or "nafta_3" or "diesel_2" or "diesel_3") ||
            v.TankLitres is <= 0 or > 200 || v.ConsumptionLPer100Km is <= 0 or > 60 ||
            input.InitialLitres < 0 || input.InitialLitres > v.TankLitres ||
            input.ReserveLitres < 0 || input.ReserveLitres >= v.TankLitres ||
            input.ManualPriceArsPerLitre is <= 0 or > 1000000)
            throw new DomainException("invalid_vehicle", "Revisá consumo, tanque, combustible inicial, reserva y precio.");
        if (input.Pauses.Length > 20 || input.Pauses.Any(p => p is null || !double.IsFinite(p.DistanceM) || p.DistanceM < 0 || p.DurationSeconds is <= 0 or > 86400))
            throw new DomainException("invalid_pauses", "Se admiten hasta 20 pausas de máximo 24 horas, con distancia válida.");
    }
}
