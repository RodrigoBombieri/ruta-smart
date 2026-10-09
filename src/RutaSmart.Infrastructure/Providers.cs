using System.Net.Http.Json;
using System.Text.Json;
using NetTopologySuite.IO;
using RutaSmart.Core;

namespace RutaSmart.Infrastructure;

public sealed record RoutingOptions(bool Enabled, string ApiKey, string CoverageWkt, bool AllowDemo, string Provider = "ors");

public sealed class RoutingProvider(HttpClient http, RoutingOptions options)
{
    static readonly SemaphoreSlim PublicGate = new(1,1);
    static DateTimeOffset lastPublicRequest;
    public async Task<RouteData> Route(TripInput input, CancellationToken ct)
    {
        if (input.Demo)
        {
            if (!options.AllowDemo) throw new DomainException("demo_disabled", "Demostración deshabilitada en este servidor.", 403);
            var a = input.Origin.Point; var b = input.Destination.Point;
            var points = Enumerable.Range(0, 21).Select(i => new Point(a.Latitude + (b.Latitude-a.Latitude)*i/20,
                a.Longitude + (b.Longitude-a.Longitude)*i/20 + Math.Sin(i*Math.PI/20)*.04)).ToArray();
            var distance = Geo.Distance(a, b)*1.2;
            return new("demo", true, points, distance, distance/22.22, "Datos sintéticos de RutaSmart",
                ["Trazado ilustrativo sin correspondencia con carreteras.", "Distancia y tiempos ficticios; no usar para viajar.", "ETA proporcional por distancia, baja confianza."]);
        }
        if (options.Provider=="osrm_public" && options.AllowDemo) return await PublicOsrm(input,ct);
        if (!options.Enabled || string.IsNullOrWhiteSpace(options.ApiKey) || string.IsNullOrWhiteSpace(options.CoverageWkt))
            throw new DomainException("routing_not_configured", "Routing real pendiente de clave, licencia y área de cobertura aprobada. Podés usar la demostración.", 503);
        var area = new WKTReader().Read(options.CoverageWkt);
        if (!area.IsValid || area.Dimension != NetTopologySuite.Geometries.Dimension.Surface)
            throw new DomainException("routing_configuration", "El área de cobertura no es válida.", 503);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openrouteservice.org/v2/directions/driving-car/geojson");
        request.Headers.TryAddWithoutValidation("Authorization", options.ApiKey);
        request.Content = JsonContent.Create(new { coordinates = new[] {
            new[] { input.Origin.Point.Longitude, input.Origin.Point.Latitude },
            new[] { input.Destination.Point.Longitude, input.Destination.Point.Latitude } },
            instructions = false, options = new { avoid_borders = "all" } });
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode) throw new DomainException("routing_unavailable", "El proveedor no pudo entregar una ruta. Intentá más tarde.", 503);
        await response.Content.LoadIntoBufferAsync(8_000_000, ct);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        try
        {
            var feature = json.RootElement.GetProperty("features")[0];
            var coords = feature.GetProperty("geometry").GetProperty("coordinates");
            if (coords.GetArrayLength() is < 2 or > 100000) throw new JsonException();
            var points = coords.EnumerateArray().Select(p => new Point(p[1].GetDouble(), p[0].GetDouble())).ToArray();
            if (points.Any(p => !double.IsFinite(p.Latitude) || !double.IsFinite(p.Longitude)) || !area.Covers(Geo.Line(points)))
                throw new DomainException("route_outside_coverage", "La ruta sale del área de cobertura aprobada.");
            if (Geo.Distance(points[0], input.Origin.Point) > 2000 || Geo.Distance(points[^1], input.Destination.Point) > 2000)
                throw new DomainException("route_snap_too_far", "El proveedor ubicó un extremo demasiado lejos del punto confirmado.");
            var summary = feature.GetProperty("properties").GetProperty("summary");
            var distance = summary.GetProperty("distance").GetDouble();
            var duration = summary.GetProperty("duration").GetDouble();
            if (!double.IsFinite(distance) || !double.IsFinite(duration) || distance is <= 0 or > 8000000 || duration is <= 0 or > 604800)
                throw new JsonException();
            return new("openrouteservice", false, points, distance, duration, "© openrouteservice · © OpenStreetMap contributors",
                ["ETA proporcional a la distancia: baja confianza, sin tráfico en vivo.", "La navegación externa puede elegir otra ruta.", "Estado vial no habilitado; alertas SMN con cobertura parcial."]);
        }
        catch (Exception e) when (e is JsonException or KeyNotFoundException or IndexOutOfRangeException or InvalidOperationException)
        { throw new DomainException("invalid_route_response", "El proveedor devolvió una ruta no interpretable.", 503); }
    }
    async Task<RouteData> PublicOsrm(TripInput input,CancellationToken ct)
    {
        // Small inland pilot corridor; it does not imply national coverage or road-state knowledge.
        const string pilot="POLYGON ((-61 -35,-58.35 -35,-58.35 -32.5,-61 -32.5,-61 -35))";
        var area=new WKTReader().Read(pilot);
        foreach (var location in new[] { input.Origin,input.Destination })
            if (!area.Covers(new NetTopologySuite.Geometries.Point(location.Point.Longitude,location.Point.Latitude)))
                throw new DomainException("outside_pilot", "El routing público de prueba está limitado al corredor Buenos Aires–Rosario y alrededores. Para otros recorridos configurá ORS o usá demostración sintética.");
        var url=FormattableString.Invariant($"https://router.project-osrm.org/route/v1/driving/{input.Origin.Point.Longitude},{input.Origin.Point.Latitude};{input.Destination.Point.Longitude},{input.Destination.Point.Latitude}?overview=full&geometries=geojson&steps=false");
        await PublicGate.WaitAsync(ct);
        HttpResponseMessage response;
        try
        {
            var wait=lastPublicRequest.AddMilliseconds(1100)-DateTimeOffset.UtcNow;
            if (wait>TimeSpan.Zero) await Task.Delay(wait,ct);
            lastPublicRequest=DateTimeOffset.UtcNow;
            response=await http.GetAsync(url,HttpCompletionOption.ResponseHeadersRead,ct);
        }
        finally { PublicGate.Release(); }
        using (response)
        {
            if (!response.IsSuccessStatusCode) throw new DomainException("routing_unavailable","El servicio de routing de prueba no está disponible.",503);
            await response.Content.LoadIntoBufferAsync(8_000_000,ct);
            using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            if (json.RootElement.GetProperty("code").GetString()!="Ok") throw new DomainException("no_route","No se encontró una ruta utilizable.",503);
            var route=json.RootElement.GetProperty("routes")[0];
            var coordinates=route.GetProperty("geometry").GetProperty("coordinates");
            if (coordinates.GetArrayLength() is < 2 or > 100000) throw new DomainException("invalid_route","Geometría de ruta inválida.",503);
            var points=coordinates.EnumerateArray().Select(p=>new Point(p[1].GetDouble(),p[0].GetDouble())).ToArray();
            if (!area.Covers(Geo.Line(points))) throw new DomainException("route_outside_coverage","La ruta sale del corredor piloto.");
            if (Geo.Distance(points[0],input.Origin.Point)>2000 || Geo.Distance(points[^1],input.Destination.Point)>2000)
                throw new DomainException("route_snap_too_far","El acceso a la ruta está demasiado lejos del punto confirmado.");
            var distance=route.GetProperty("distance").GetDouble(); var duration=route.GetProperty("duration").GetDouble();
            if (!double.IsFinite(distance) || !double.IsFinite(duration) || distance is <=0 or >1000000 || duration is <=0 or >172800)
                throw new DomainException("invalid_route","Distancia o duración del proveedor inválida.",503);
            return new("osrm_public",false,points,distance,duration,"OSRM / FOSSGIS · © OpenStreetMap contributors (ODbL)",
                ["Servicio público de prueba, solo uso no comercial. Sin garantía de disponibilidad ni actualización.",
                 "Cobertura piloto Buenos Aires–Rosario y alrededores, no nacional.",
                 "ETA proporcional a la distancia, baja confianza. No incluye tráfico en vivo.","La navegación externa puede elegir otra ruta."]);
        }
    }
}

public sealed class LocationProvider(HttpClient http)
{
    public async Task<Location[]> Search(string query, CancellationToken ct)
    {
        if (query.Length is < 2 or > 80) throw new DomainException("invalid_query", "Ingresá entre 2 y 80 caracteres.");
        using var response = await http.GetAsync("https://apis.datos.gob.ar/georef/api/localidades?max=10&nombre=" + Uri.EscapeDataString(query), ct);
        if (!response.IsSuccessStatusCode) throw new DomainException("georef_unavailable", "GeoRef no está disponible. Podés ingresar coordenadas manualmente.", 503);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        return json.RootElement.GetProperty("localidades").EnumerateArray().Select(l => new Location(
            l.GetProperty("nombre").GetString() + ", " + l.GetProperty("provincia").GetProperty("nombre").GetString(),
            new Point(l.GetProperty("centroide").GetProperty("lat").GetDouble(), l.GetProperty("centroide").GetProperty("lon").GetDouble()),
            l.GetProperty("id").GetString())).ToArray();
    }
}
