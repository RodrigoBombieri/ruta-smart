using RutaSmart.Core;
using RutaSmart.Infrastructure;
namespace RutaSmart.Tests;
public class EnergyTests
{
    const string Header="idempresa,empresa,direccion,localidad,provincia,idproducto,tipohorario,precio,fecha_vigencia,empresabandera,latitud,longitud\n";
    const string Row="1,Estación,Dirección,Rosario,Santa Fe,2,Diurno,1500,2026-10-07 08:00:00,Marca,-33,-60\n";
    static readonly DateTimeOffset Now=new(2026,10,8,12,0,0,TimeSpan.Zero);
    [Fact] public async Task RestoredInventoryBecomesStaleOnlyAfterSevenDays()
    {
        var store=new MemoryStore();
        var data=EnergyParser.Parse(Header+Row,Now,"test");
        await store.WriteSource("energia-stations",System.Text.Json.JsonSerializer.Serialize(data),default);
        using var http=new HttpClient();
        var provider=new EnergyProvider(http,store,new(null,false));
        Assert.Equal("unavailable",provider.Coverage(Now).Availability);
        await provider.Restore(default);
        Assert.Equal("partial",provider.Coverage(Now).Availability);
        Assert.Equal("partial",provider.Coverage(Now.AddDays(7)).Availability);
        Assert.Equal("stale",provider.Coverage(Now.AddDays(7).AddSeconds(1)).Availability);
    }
    [Fact] public void DeduplicatesStationButPreservesSchedulePrices()
    {
        var data=EnergyParser.Parse(Header+Row+Row.Replace("Diurno","Nocturno"),Now,"test");
        Assert.Single(data.Stations); Assert.Equal(2,data.Stations[0].Prices.Length);
        Assert.Equal("nafta_2",data.Stations[0].FuelType); Assert.Equal(TimeSpan.FromHours(-3),data.Stations[0].Prices[0].DeclaredAt.Offset);
    }
    [Fact] public void RejectsBadSchemaAndInvalidCoordinates()
    {
        Assert.Throws<FormatException>(()=>EnergyParser.Parse("new_schema\nvalue",Now,"test"));
        Assert.Throws<FormatException>(()=>EnergyParser.Parse(Header+Row.Replace("-33,-60","0,0"),Now,"test"));
    }
    [Fact] public void AncientPriceIsPreservedButNotComparable()
    {
        var data=EnergyParser.Parse(Header+Row.Replace("2026-10-07","2020-10-07"),Now,"test");
        Assert.Equal(2020,data.Stations[0].Prices[0].DeclaredAt.Year);
        Assert.Null(StationEngine.ComparablePrice(data.Stations[0].Prices[0],Now));
    }
    [Fact] public void GnsIsExcludedFromLitresModel()
    { var data=EnergyParser.Parse(Header+Row+Row.Replace(",2,",",6,"),Now,"test"); Assert.Equal(1,data.ExcludedProducts); Assert.Single(data.Stations); }
    [Fact] public void ProximityDoesNotClaimRoadAccessOrRefuelingPlan()
    {
        var report=AssessmentEngine.Build(Guid.NewGuid(),1,DomainTests.Input(),DomainTests.Route() with { Synthetic=false },Now);
        var station=EnergyParser.Parse(Header+Row,Now,"test").Stations;
        var candidate=Assert.Single(StationEngine.Candidates(report,station));
        Assert.Equal("not_verified",candidate.AccessStatus);
    }
    [Fact] public void CandidateLimitPreservesStationsLaterInTheJourney()
    {
        var route=DomainTests.Route() with { Synthetic=false,Geometry=[new(-34,-60),new(-33,-60)],DistanceM=111000 };
        var report=AssessmentEngine.Build(Guid.NewGuid(),1,DomainTests.Input(),route,Now);
        var crowdedOrigin=Enumerable.Range(0,30).Select(i=>new FuelStation(i.ToString(),"Estación","Marca","",new(-34+i*.00001,-60),"nafta_2",[]));
        var late=new FuelStation("late","Estación final","Marca","",new(-33.1,-60),"nafta_2",[]);
        var candidates=StationEngine.Candidates(report,crowdedOrigin.Append(late));
        Assert.Equal(20,candidates.Length);
        Assert.Contains(candidates,c=>c.Station.Id=="late");
        Assert.Equal(candidates.OrderBy(c=>c.DistanceAlongM),candidates);
    }
}
