using RutaSmart.Core;
using RutaSmart.Infrastructure;
namespace RutaSmart.Tests;

public class RoadTests
{
    const string Header="filtro-provincia,filtro-ruta,filtro-tramo,estado,calzada,observaciones,actualizado\n";
    const string Row="Buenos Aires,9,Campana - límite Santa Fe,<span>CORTE TOTAL</span>,PAVIMENTO,Obras &amp; desvío,9/10/2026 09:00\n";
    static readonly DateTimeOffset Now=new(2026,10,9,18,0,0,TimeSpan.Zero);
    [Fact] public void ParsesOfficialCsvWithoutRenderingHtmlAndPreservesDate()
    {
        var snapshot=VialidadParser.Parse(Header+Row,Now);
        var notice=Assert.Single(snapshot.Notices);
        Assert.Equal("CORTE TOTAL",notice.Status);
        Assert.Equal("Obras & desvío",notice.Description);
        Assert.Equal(TimeSpan.FromHours(-3),notice.UpdatedAt!.Value.Offset);
        Assert.Throws<FormatException>(()=>VialidadParser.Parse("unexpected\nvalue",Now));
    }
    [Fact] public void RoadAndProvinceAreRequiredAndClosureRemainsApproximate()
    {
        var road=new RouteRoad("9","Buenos Aires",1000,100000);
        var route=DomainTests.Route() with { Synthetic=false,Roads=[road] };
        var report=AssessmentEngine.Build(Guid.NewGuid(),1,DomainTests.Input(),route,Now);
        var notices=VialidadParser.Parse(Header+Row+Row.Replace("Buenos Aires","Salta"),Now).Notices;
        var result=RoadEngine.Apply(report,notices,new("roads","partial","Parte vial"),Now);
        Assert.Single(result.RoadNotices);
        Assert.Equal("caution",result.Status);
        Assert.Equal("road_and_province_only",result.RoadNotices[0].Confidence);
        Assert.Empty(RoadEngine.Apply(report with { Route=route with { Roads=[] } },notices,new("roads","partial",""),Now).RoadNotices);
        Assert.Empty(RoadEngine.Apply(report with { Route=route with { Synthetic=true } },notices,new("roads","partial",""),Now).RoadNotices);
    }
    [Fact] public void OldRoadNoticeIsNotRejuvenatedByDownload()
    {
        var notice=VialidadParser.Parse(Header+Row.Replace("9/10/2026","1/10/2026"),Now).Notices;
        var report=AssessmentEngine.Build(Guid.NewGuid(),1,DomainTests.Input(),DomainTests.Route() with { Synthetic=false,Roads=[new("9","Buenos Aires",0,100)] },Now);
        Assert.True(Assert.Single(RoadEngine.Apply(report,notice,new("roads","partial",""),Now).RoadNotices).Stale);
    }
    [Theory]
    [InlineData("RN9","9")]
    [InlineData("RNA001","A1")]
    [InlineData("RP9",null)]
    [InlineData("9",null)]
    public void NationalAndProvincialRoadsAreNotConfused(string input,string? expected)=>Assert.Equal(expected,ProvinceCatalog.NationalRoad(input));
    [Fact] public void ProvincialBoundariesIdentifyActualArgentineLocations()
    {
        Assert.Equal("Córdoba",ProvinceCatalog.Shared.Province(new(-31.4167,-64.1833)));
        Assert.Equal("Mendoza",ProvinceCatalog.Shared.Province(new(-32.89,-68.84)));
        Assert.Null(ProvinceCatalog.Shared.Province(new(-33.45,-70.66))); // Santiago, Chile
        Assert.NotNull(ProvinceCatalog.Shared.Province(new(-54.8,-68.3))); // Ushuaia
    }
}
