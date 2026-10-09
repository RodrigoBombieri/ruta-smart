namespace RutaSmart.Core;

public static class StationEngine
{
    public static StationCandidate[] Candidates(Assessment assessment,IEnumerable<FuelStation> stations)
    {
        if (assessment.Route.Synthetic) return [];
        var route=assessment.Route; var result=new List<StationCandidate>();
        var lengths=route.Geometry.Zip(route.Geometry.Skip(1),Geo.Distance).ToArray();
        var cumulative=new double[lengths.Length+1];
        for (var i=0;i<lengths.Length;i++) cumulative[i+1]=cumulative[i]+lengths[i];
        var total=cumulative[^1];
        if (total<=0) return [];
        var latMin=route.Geometry.Min(p=>p.Latitude)-.05; var latMax=route.Geometry.Max(p=>p.Latitude)+.05;
        var lonPadding=.06/Math.Cos(route.Geometry.Average(p=>p.Latitude)*Math.PI/180);
        var lonMin=route.Geometry.Min(p=>p.Longitude)-lonPadding; var lonMax=route.Geometry.Max(p=>p.Longitude)+lonPadding;
        foreach (var station in stations.Where(s=>s.FuelType==assessment.Input.Vehicle.FuelType && s.Point.Latitude>=latMin && s.Point.Latitude<=latMax && s.Point.Longitude>=lonMin && s.Point.Longitude<=lonMax))
        {
            double best=double.MaxValue, progress=0;
            var cos=Math.Cos(station.Point.Latitude*Math.PI/180);
            for (var i=0;i<lengths.Length;i++)
            {
                var a=route.Geometry[i]; var b=route.Geometry[i+1];
                var ax=(a.Longitude-station.Point.Longitude)*cos; var ay=a.Latitude-station.Point.Latitude;
                var dx=(b.Longitude-a.Longitude)*cos; var dy=b.Latitude-a.Latitude;
                var fraction=dx*dx+dy*dy==0 ? 0 : Math.Clamp(-(ax*dx+ay*dy)/(dx*dx+dy*dy),0,1);
                var projection=new Point(a.Latitude+(b.Latitude-a.Latitude)*fraction,a.Longitude+(b.Longitude-a.Longitude)*fraction);
                var distance=Geo.Distance(station.Point,projection);
                if (distance<best) { best=distance; progress=(cumulative[i]+lengths[i]*fraction)/total*route.DistanceM; }
            }
            if (best<=5000) result.Add(new(station,progress,best,"not_verified",progress/1000 <= (double)assessment.Fuel.InitialRangeKm));
        }
        // Candidate discovery only. Lateral distance is never a driving detour or proof of accessibility.
        // Spread candidates across the journey instead of filling every slot near the origin.
        var selected=result.GroupBy(s=>Math.Min(19,(int)(s.DistanceAlongM/route.DistanceM*20)))
            .Select(g=>g.OrderBy(s=>s.LateralDistanceM).ThenBy(s=>s.Station.Id).First()).ToList();
        selected.AddRange(result.Except(selected).OrderBy(s=>s.LateralDistanceM).ThenBy(s=>s.Station.Id).Take(20-selected.Count));
        return selected.OrderBy(s=>s.DistanceAlongM).ThenBy(s=>s.LateralDistanceM).ToArray();
    }
    public static decimal? ComparablePrice(StationPrice price,DateTimeOffset now) =>
        price.AmountArsPerLitre>0 && price.DeclaredAt<=now && price.DeclaredAt>=now.AddDays(-7) ? price.AmountArsPerLitre : null;
}
