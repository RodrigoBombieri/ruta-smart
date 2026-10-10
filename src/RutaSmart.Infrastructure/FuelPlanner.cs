using System.Text.Json;
using RutaSmart.Core;

namespace RutaSmart.Infrastructure;

public sealed partial class RoutingProvider
{
    public async Task<FuelPlan> FuelPlan(Assessment report, DateTimeOffset now, CancellationToken ct)
    {
        FuelPlan Empty(string status,string message)=>new(status,message,now,null,[],0,0,0);
        if(report.Route.Synthetic || options.Provider!="osrm_public" || !options.AllowDemo)
            return Empty("unavailable","El plan por carretera requiere routing OSRM real habilitado.");
        if(report.Fuel.MinimumPurchaseLitres==0) return Empty("not_needed","El combustible inicial alcanza conservando la reserva estimada.");
        if(report.FuelPlan?.Status=="accepted") return Empty("unavailable","Volvé a planificar para cambiar las paradas de un plan aceptado.");
        var candidates=report.Stations.Take(20).Select(c=>c.Station).ToArray();
        if(candidates.Length==0) return Empty("no_feasible_plan","No hay estaciones candidatas compatibles en este informe.");
        Point[] points=[report.Input.Origin.Point,..candidates.Select(s=>s.Point),report.Input.Destination.Point];
        var text=string.Join(";",points.Select(p=>FormattableString.Invariant($"{p.Longitude},{p.Latitude}")));
        await PublicGate.WaitAsync(ct);
        HttpResponseMessage response;
        try
        {
            var wait=lastPublicRequest.AddMilliseconds(1100)-DateTimeOffset.UtcNow;
            if(wait>TimeSpan.Zero) await Task.Delay(wait,ct);
            lastPublicRequest=DateTimeOffset.UtcNow;
            response=await http.GetAsync($"https://router.project-osrm.org/table/v1/driving/{text}?annotations=distance",HttpCompletionOption.ResponseHeadersRead,ct);
        }
        finally { PublicGate.Release(); }
        using(response)
        {
            if(!response.IsSuccessStatusCode) throw new DomainException("fuel_routing_unavailable","No se pudieron consultar los accesos por carretera. Intentá más tarde.",503);
            await response.Content.LoadIntoBufferAsync(2_000_000,ct);
            using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var root=json.RootElement;
            if(root.GetProperty("code").GetString()!="Ok") throw new DomainException("fuel_routing_unavailable","El proveedor no entregó accesos por carretera.",503);
            var matrix=root.GetProperty("distances").EnumerateArray().Select(row=>row.EnumerateArray().Select(d=>d.ValueKind==JsonValueKind.Null?(double?)null:d.GetDouble()).ToArray()).ToArray();
            if(matrix.Length!=points.Length || matrix.Any(row=>row.Length!=points.Length)) throw new JsonException();
            foreach(var field in new[]{"sources","destinations"})
            {
                var snaps=root.GetProperty(field);
                if(snaps.GetArrayLength()!=points.Length) throw new JsonException();
                for(var i=0;i<points.Length;i++)
                {
                    var loc=snaps[i].GetProperty("location");
                    var distance=Geo.Distance(points[i],new Point(loc[1].GetDouble(),loc[0].GetDouble()));
                    if(!double.IsFinite(distance) || distance>(i==0 || i==points.Length-1?2000:150))
                        for(var j=0;j<points.Length;j++) { matrix[i][j]=null; matrix[j][i]=null; }
                }
            }
            var path=FuelPlanEngine.FindStops(report.Input,matrix,Math.Min(8,20-report.Input.Pauses.Length));
            if(path is null) return Empty("no_feasible_plan","No se encontró un plan entre las 20 candidatas, con la reserva indicada y hasta 8 cargas. Puede haber otras estaciones fuera del catálogo seleccionado.");
            var stations=path.Select(i=>candidates[i-1]).ToArray();
            var route=await PublicOsrm(report.Input,ct,stations.Select(s=>s.Point).ToArray());
            return FuelPlanEngine.Build(report,route,stations,now);
        }
    }
}
