namespace RutaSmart.Core;

public static class FuelPlanEngine
{
    // Each station resets available range to a full tank. Directed edges preserve road access.
    public static int[]? FindStops(TripInput input, double?[][] distances, int maxStops)
    {
        var n=distances.Length;
        var initial=(double)Math.Max(0,input.InitialLitres-input.ReserveLitres)*100000/(double)input.Vehicle.ConsumptionLPer100Km;
        var full=(double)(input.Vehicle.TankLitres-input.ReserveLitres)*100000/(double)input.Vehicle.ConsumptionLPer100Km;
        var states=new Dictionary<int,(double Distance,int[] Path)> { [0]=(0,[]) };
        for(var count=0;count<=maxStops;count++)
        {
            var next=new Dictionary<int,(double Distance,int[] Path)>();
            (double Distance,int[] Path)? best=null;
            foreach(var (from,state) in states)
            for(var to=1;to<n;to++)
            {
                if(to==from || state.Path.Contains(to) || distances[from][to] is not double d || !double.IsFinite(d) || d<0 || d>(from==0?initial:full)) continue;
                var cost=state.Distance+d;
                if(to==n-1) { if(best is null || cost<best.Value.Distance) best=(cost,state.Path); }
                else if(count<maxStops && (!next.TryGetValue(to,out var existing) || cost<existing.Distance)) next[to]=(cost,[..state.Path,to]);
            }
            if(best is not null) return best.Value.Path;
            states=next;
        }
        return null;
    }

    public static FuelPlan Build(Assessment report, RouteData route, FuelStation[] stations, DateTimeOffset now)
    {
        var input=report.Input;
        if(route.Legs.Length!=stations.Length+1 || route.Legs.Any(l=>!double.IsFinite(l.DistanceM) || !double.IsFinite(l.DurationSeconds) || l.DistanceM<0 || l.DurationSeconds<0) ||
            Math.Abs(route.Legs.Sum(l=>l.DistanceM)-route.DistanceM)>10 || route.DistanceM>report.Route.DistanceM*1.3+30000)
            throw new DomainException("fuel_route_invalid","No se pudo validar un plan con desvíos razonables y autonomía suficiente.");
        var stops=new List<FuelStop>(); var fuel=input.InitialLitres; var distance=0d; var seconds=0d;
        decimal Burn(double metres)=>(decimal)metres*input.Vehicle.ConsumptionLPer100Km/100000;
        for(var i=0;i<route.Legs.Length;i++)
        {
            var leg=route.Legs[i]; fuel-=Burn(leg.DistanceM); distance+=leg.DistanceM; seconds+=leg.DurationSeconds;
            if(fuel<input.ReserveLitres) throw new DomainException("fuel_range_insufficient","Una etapa consume la reserva: el plan no es utilizable.");
            if(i==stations.Length) break;
            var purchase=Math.Max(0,Burn(route.Legs[i+1].DistanceM)+input.ReserveLitres-fuel);
            // Round purchases upward to centilitres, never below the required reserve.
            purchase=Math.Ceiling(purchase*100)/100;
            if(fuel+purchase>input.Vehicle.TankLitres) throw new DomainException("fuel_tank_insufficient","Una etapa supera la capacidad del tanque.");
            var at=input.DepartureAt.AddSeconds(seconds+input.Pauses.Where(p=>p.DistanceM<=distance).Sum(p=>p.DurationSeconds));
            stops.Add(new(stations[i],distance,fuel,purchase,fuel+purchase,at,900));
            fuel+=purchase; seconds+=900;
        }
        return new("ready","Recorrido por carretera estimado. Confirmá entrada, horario y stock; la cartografía no verifica el acceso al surtidor.",now,route,stops.ToArray(),
            route.DistanceM-report.Route.DistanceM,route.DurationSeconds-report.Route.DurationSeconds+stops.Count*900,stops.Sum(s=>s.PurchaseLitres));
    }
}
