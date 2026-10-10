using System.Net.Http.Json;
using System.Text.Json;
using NetTopologySuite.IO;
using RutaSmart.Core;

namespace RutaSmart.Infrastructure;

public sealed record RoutingOptions(bool Enabled, string ApiKey, string CoverageWkt, bool AllowDemo, string Provider = "ors");

public sealed partial class RoutingProvider(HttpClient http, RoutingOptions options)
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
                ["ETA proporcional a la distancia: baja confianza, sin tráfico en vivo.", "La navegación externa puede elegir otra ruta.", "Este adaptador no entrega referencias RN para vincular los partes de Vialidad; alertas SMN con cobertura parcial."]);
        }
        catch (Exception e) when (e is JsonException or KeyNotFoundException or IndexOutOfRangeException or InvalidOperationException)
        { throw new DomainException("invalid_route_response", "El proveedor devolvió una ruta no interpretable.", 503); }
    }
    async Task<RouteData> PublicOsrm(TripInput input,CancellationToken ct, Point[]? via = null)
    {
        var geography=ProvinceCatalog.Shared;
        foreach (var location in new[] { input.Origin,input.Destination })
            if (!geography.Contains(location.Point))
                throw new DomainException("outside_argentina", "Confirmá origen y destino dentro de Argentina. Los tramos internacionales intermedios se informan por separado.");
        Point[] locations=[input.Origin.Point,..via ?? [],input.Destination.Point];
        var coordinatesText=string.Join(";",locations.Select(p=>FormattableString.Invariant($"{p.Longitude},{p.Latitude}")));
        var url=$"https://router.project-osrm.org/route/v1/driving/{coordinatesText}?overview=full&geometries=geojson&steps=true&continue_straight=false";
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
            var waypoints=json.RootElement.GetProperty("waypoints");
            if(waypoints.GetArrayLength()!=locations.Length) throw new JsonException();
            for(var i=0;i<locations.Length;i++)
            {
                var snap=waypoints[i].GetProperty("location");
                if(Geo.Distance(locations[i],new Point(snap[1].GetDouble(),snap[0].GetDouble()))>(i==0 || i==locations.Length-1 ? 2000 : 150))
                    throw new DomainException("station_access_uncertain","El acceso cartográfico a una parada queda a más de 150 m de la estación.");
            }
            var coordinates=route.GetProperty("geometry").GetProperty("coordinates");
            if (coordinates.GetArrayLength() is < 2 or > 100000) throw new DomainException("invalid_route","Geometría de ruta inválida.",503);
            var points=coordinates.EnumerateArray().Select(p=>new Point(p[1].GetDouble(),p[0].GetDouble())).ToArray();
            if(points.Any(p=>!double.IsFinite(p.Latitude) || !double.IsFinite(p.Longitude) || p.Latitude is < -90 or >90 || p.Longitude is < -180 or >180))
                throw new DomainException("invalid_route","Coordenadas del proveedor inválidas.",503);
            if (Geo.Distance(points[0],input.Origin.Point)>2000 || Geo.Distance(points[^1],input.Destination.Point)>2000)
                throw new DomainException("route_snap_too_far","El acceso a la ruta está demasiado lejos del punto confirmado.");
            var distance=route.GetProperty("distance").GetDouble(); var duration=route.GetProperty("duration").GetDouble();
            if (!double.IsFinite(distance) || !double.IsFinite(duration) || distance is <=0 or >8000000 || duration is <=0 or >604800)
                throw new DomainException("invalid_route","Distancia o duración del proveedor inválida.",503);
            var crossesBorder=geography.CrossesBorder(points);
            return new RouteData("osrm_public",false,points,distance,duration,"OSRM / FOSSGIS · © OpenStreetMap contributors (ODbL)",
                ["Servicio público de prueba, solo uso no comercial. Sin garantía de disponibilidad ni actualización.",
                 crossesBorder ? "El trazado incluye tránsito internacional o puntos fuera de la cartografía argentina. Verificá fronteras, documentación y eventuales ferris; no están validados." : "Routing habilitado en Argentina; disponibilidad sujeta al grafo vial del proveedor.",
                 "ETA estimada, baja confianza. No incluye tráfico en vivo.","La navegación externa puede elegir otra ruta."]) { Roads=ReadRoads(route),CrossesInternationalBorder=crossesBorder,
                 Legs=route.GetProperty("legs").EnumerateArray().Select(l=>new RouteLeg(l.GetProperty("distance").GetDouble(),l.GetProperty("duration").GetDouble())).ToArray() };
        }
    }
    public static RouteRoad[] ReadRoads(JsonElement route)
    {
        var result=new List<RouteRoad>(); var offset=0d;
        if(!route.TryGetProperty("legs",out var legs)) return [];
        foreach(var leg in legs.EnumerateArray())
        foreach(var step in leg.GetProperty("steps").EnumerateArray())
        {
            var distance=step.GetProperty("distance").GetDouble();
            if(!double.IsFinite(distance) || distance<0) throw new JsonException("Invalid step distance");
            if(step.TryGetProperty("ref",out var reference) && reference.ValueKind==JsonValueKind.String)
            {
                var points=step.GetProperty("geometry").GetProperty("coordinates").EnumerateArray().Select(p=>new Point(p[1].GetDouble(),p[0].GetDouble())).ToArray();
                result.AddRange(ProvinceCatalog.Shared.LocateRoad(reference.GetString()!,points,offset,distance));
            }
            offset+=distance;
        }
        return result.ToArray();
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
