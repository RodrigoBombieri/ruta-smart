using RutaSmart.Core;
namespace RutaSmart.Tests;

public class FuelPlanTests
{
    static TripInput Input()=>DomainTests.Input() with { InitialLitres=15,ReserveLitres=5,Vehicle=new("Auto","nafta_2",40,10),Demo=false };
    [Fact] public void DirectedRoadDistancesAndReserveDetermineReachability()
    {
        double?[][] matrix=[[0,90000,200000,450000],[null,0,180000,360000],[null,180000,0,180000],[null,null,null,0]];
        Assert.Equal(new[]{1,2},FuelPlanEngine.FindStops(Input(),matrix,8));
        matrix[0][1]=110000; // Nearby station cannot be reached on the actual road network.
        Assert.Null(FuelPlanEngine.FindStops(Input(),matrix,8));
    }
    [Fact] public void StopLimitAndDisconnectedRoadsDoNotProduceFakePlans()
    {
        double?[][] matrix=[[0,90000,null],[null,0,200000],[null,null,0]];
        Assert.Null(FuelPlanEngine.FindStops(Input(),matrix,0));
        Assert.Equal(new[]{1},FuelPlanEngine.FindStops(Input(),matrix,1));
        matrix[1][2]=null;
        Assert.Null(FuelPlanEngine.FindStops(Input(),matrix,8));
    }
    [Fact] public void FinalRouteRechecksTankAndComputesPurchasesAndPauses()
    {
        var input=Input();
        var route=DomainTests.Route() with { Synthetic=false,DistanceM=290000,DurationSeconds=10000,Legs=[new(90000,3000),new(200000,7000)] };
        var report=AssessmentEngine.Build(Guid.NewGuid(),1,input,route,DateTimeOffset.UtcNow);
        var station=new FuelStation("1","Estación","Marca","Dirección",new(-33,-60),"nafta_2",[]);
        var plan=FuelPlanEngine.Build(report,route,[station],DateTimeOffset.UtcNow);
        var stop=Assert.Single(plan.Stops);
        Assert.Equal(6,stop.ArrivalLitres); Assert.Equal(19,stop.PurchaseLitres); Assert.Equal(25,stop.DepartureLitres);
        Assert.Equal(900,plan.ExtraSeconds);
        Assert.Equal(input.DepartureAt.AddSeconds(3000),stop.ArrivalAt);
        Assert.Throws<DomainException>(()=>FuelPlanEngine.Build(report,route with { Legs=[new(110000,3000),new(180000,7000)] },[station],DateTimeOffset.UtcNow));
    }
    [Fact] public void TimelineUsesActualLegDurations()
    {
        var route=DomainTests.Route() with { DistanceM=200000,DurationSeconds=10000,Legs=[new(100000,2000),new(100000,8000)] };
        Assert.Equal(Input().DepartureAt.AddSeconds(2000).ToUnixTimeSeconds(),TimelineEngine.At(Input(),route,100000).At.ToUnixTimeSeconds());
    }
}
