using NetTopologySuite.IO;
using RutaSmart.Core;
using RutaSmart.Infrastructure;

namespace RutaSmart.Tests;

public class DomainTests
{
    public static TripInput Input() => new(new("Buenos Aires",new(-34.6037,-58.3816)),
        new("Rosario",new(-32.9468,-60.6393)), DateTimeOffset.UtcNow.AddDays(1).ToOffset(TimeSpan.FromHours(-3)),
        "America/Argentina/Buenos_Aires",new("Auto","nafta_2",47,7.5m),30,5,null,[],true);
    public static RouteData Route() => new("test",true,[new(-34,-58),new(-33,-60)],1200000,54000,"synthetic",[]);

    [Fact] public void FuelSeparatesConsumptionAndPurchases()
    {
        var fuel = FuelEngine.Calculate(Input() with { ManualPriceArsPerLitre=1500 },1200000);
        Assert.Equal(90,fuel.ConsumedLitres); Assert.Equal(65,fuel.MinimumPurchaseLitres);
        Assert.Equal(135000,fuel.ConsumptionCostArs); Assert.Equal(97500,fuel.MinimumPurchaseCostArs);
        Assert.Equal(333.333m,Math.Round(fuel.InitialRangeKm,3));
    }
    [Fact] public void MissingPriceIsNullNotZero()
    { var f=FuelEngine.Calculate(Input(),1200000); Assert.Null(f.ConsumptionCostArs); Assert.Null(f.MinimumPurchaseCostArs); Assert.Equal(90,f.ConsumedLitres); }
    [Fact] public void FuelBelowReserveHasNoUsableRange()
    { Assert.Equal(0,FuelEngine.Calculate(Input() with { InitialLitres=2 },10000).InitialRangeKm); }
    [Fact] public void FullTankRangeExcludesReserve()
    { Assert.Equal(560,FuelEngine.Calculate(Input() with { InitialLitres=47 },10000).InitialRangeKm); }
    [Theory] [InlineData(0,47,5,30)] [InlineData(7.5,47,47,30)] [InlineData(7.5,47,5,48)] [InlineData(7.5,47,-1,30)]
    public void InvalidVehicleRejected(decimal consumption, decimal tank, decimal reserve, decimal initial)
    { var i=Input() with { Vehicle=new("Auto","nafta_2",tank,consumption),ReserveLitres=reserve,InitialLitres=initial }; Assert.Throws<DomainException>(()=>InputValidator.Validate(i,DateTimeOffset.UtcNow)); }
    [Fact] public void SameOriginRejected()
    { var i=Input(); Assert.Equal("same_origin_destination",Assert.Throws<DomainException>(()=>InputValidator.Validate(i with { Destination=i.Origin },DateTimeOffset.UtcNow)).Code); }
    [Fact] public void WrongOffsetRejected()
    { var i=Input() with { DepartureAt=DateTimeOffset.UtcNow.AddDays(1) }; Assert.Equal("invalid_offset",Assert.Throws<DomainException>(()=>InputValidator.Validate(i,DateTimeOffset.UtcNow)).Code); }
    [Fact] public void MissingCriticalCoverageCannotBecomeFavorable()
    { Assert.Equal("insufficient_information",AssessmentEngine.Status([],[])); }
    [Fact] public void PriceIsNotCriticalForConditions()
    { Assert.Equal("favorable",AssessmentEngine.Status([],[new("roads","available",""),new("weather","available",""),new("prices","unavailable","")])); }
    [Fact] public void KnownClosureWinsOverMissingWeather()
    { Assert.Equal("adverse",AssessmentEngine.Status([new("closure","roads","adverse","Corte","source","v1",0,50,null,null,"confirmed")],[new("weather","unavailable","")])); }
    [Fact] public void TimelineIncludesPauseAndCrossesMidnight()
    {
        var input=Input() with { DepartureAt=new DateTimeOffset(2026,10,10,23,0,0,TimeSpan.FromHours(-3)),Pauses=[new(600000,3600)] };
        var result=TimelineEngine.Build(input,Route());
        Assert.Equal(11,result[^1].At.Day); Assert.Equal(15,result[^1].At.Hour);
        Assert.Equal(input.DepartureAt.AddHours(8.5),TimelineEngine.At(input,Route(),600000).At);
        Assert.True(result.Zip(result.Skip(1)).All(x=>x.First.At<=x.Second.At));
    }
    [Fact] public void PauseAfterDestinationRejected()
    { Assert.Throws<DomainException>(()=>TimelineEngine.Build(Input() with { Pauses=[new(2000000,600)] },Route())); }
    [Fact] public void ExpiredWeatherDoesNotMatch()
    { var p=TimelineEngine.At(Input(),Route(),600000); Assert.Null(TimelineEngine.Overlap(p,p,p.Earliest.AddHours(-2),p.Earliest)); }
    [Fact] public void UncertaintyOnlyMatchIsExplicit()
    { var p=TimelineEngine.At(Input(),Route(),600000); Assert.Equal("possible_temporal_overlap",TimelineEngine.Overlap(p,p,p.Earliest,p.At.AddMinutes(-1))); }
    [Fact] public void UnknownEndDoesNotMeanForever()
    { var p=TimelineEngine.At(Input(),Route(),600000); Assert.Equal("unknown_validity",TimelineEngine.Overlap(p,p,p.At,null)); }
    [Fact] public void PolygonHoleExcludesItsInterior()
    {
        var polygon=new WKTReader().Read("POLYGON ((0 0,10 0,10 10,0 10,0 0),(4 4,4 6,6 6,6 4,4 4))");
        var route=Route() with { Geometry=[new(5,-1),new(5,11)],DistanceM=12000 };
        var intervals=Geo.Intervals(route,polygon);
        Assert.Equal(2,intervals.Length); Assert.Equal(8000,intervals.Sum(i=>i.ToM-i.FromM),3);
    }
    [Fact] public void RepeatedPassagesArePreserved()
    {
        var polygon=new WKTReader().Read("POLYGON ((0 0,2 0,2 2,0 2,0 0))");
        var route=Route() with { Geometry=[new(1,-1),new(1,3),new(1,-1)],DistanceM=8000 };
        var intervals=Geo.Intervals(route,polygon);
        Assert.Equal(2,intervals.Length); Assert.True(intervals[1].FromM>intervals[0].ToM);
    }
    [Fact] public async Task ExpiredSessionAndTripArePurged()
    {
        var store=new MemoryStore(); var now=DateTimeOffset.UtcNow;
        await store.AddSession(new("owner",now.AddSeconds(-1)),default);
        var a=AssessmentEngine.Build(Guid.NewGuid(),1,Input(),Route(),now);
        await store.Commit("owner","key","hash",a,now.AddSeconds(-1),default);
        await store.Purge(now,default);
        Assert.False(await store.HasSession("owner",now,default));
        Assert.Null(await store.Get("owner",a.TripId,now,default));
        Assert.Null(await store.GetReplay("owner","key",now,default));
    }
}
