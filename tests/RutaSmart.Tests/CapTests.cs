using RutaSmart.Core;
using RutaSmart.Infrastructure;

namespace RutaSmart.Tests;
public class CapTests
{
    const string Xml="""
    <alert xmlns="urn:oasis:names:tc:emergency:cap:1.2">
      <identifier>test-1</identifier><sender>smn@smn.gob.ar</sender><sent>2026-10-07T09:00:00-03:00</sent>
      <status>Actual</status><msgType>Alert</msgType><scope>Public</scope>
      <info><language>es-AR</language><event>Tormentas</event><severity>Severe</severity>
      <onset>2026-10-08T08:00:00-03:00</onset><expires>2026-10-08T23:00:00-03:00</expires>
      <description>Descripción de prueba sintética</description><instruction>Instrucción sintética</instruction>
      <area><polygon>-35,-61 -32,-61 -32,-58 -35,-58 -35,-61</polygon></area></info>
    </alert>
    """;
    [Fact] public void ParsesLatitudeLongitudeInCapOrder()
    {
        var a=CapParser.Parse(Xml,"https://ssl.smn.gob.ar/feeds/CAP/test.xml");
        Assert.Equal(-35,a.Polygons[0][0].Latitude); Assert.Equal(-61,a.Polygons[0][0].Longitude);
        Assert.Equal(TimeSpan.FromHours(-3),a.From.Offset); Assert.Equal("Severe",a.Severity);
    }
    [Fact] public void RejectsExternalEntityExpansion()
    { Assert.Throws<System.Xml.XmlException>(()=>CapParser.Xml("<!DOCTYPE x [<!ENTITY e SYSTEM 'file:///secret'>]><x>&e;</x>")); }
    [Fact] public void RejectsExerciseAndPrivateAlerts()
    {
        Assert.Throws<FormatException>(()=>CapParser.Parse(Xml.Replace("Actual","Exercise"),"test"));
        Assert.Throws<FormatException>(()=>CapParser.Parse(Xml.Replace("Public","Private"),"test"));
    }
    [Fact] public void IndexRestrictsHostPathAndScheme()
    {
        var links=CapParser.Links("<a href='https://ssl.smn.gob.ar/feeds/CAP/good.xml'>good</a><a href='http://127.0.0.1/secret.xml'>bad</a><a href='https://evil.example/feeds/CAP/bad.xml'>bad</a><a href='https://ssl.smn.gob.ar/private.xml'>bad</a>");
        Assert.Single(links); Assert.EndsWith("good.xml",links[0].AbsoluteUri);
    }
    [Fact] public void CancellationSurvivesMissingItemsInNextFeed()
    {
        var a=CapParser.Parse(Xml,"test"); var now=a.Sent.AddHours(1);
        var cancel=a with { Id="cancel-1",MessageType="Cancel",References=[a.Id],Sent=now };
        var merged=CapParser.Merge([a],[cancel],now);
        Assert.Empty(CapParser.Active(merged,now));
        var next=CapParser.Merge(merged,[a],now.AddHours(1));
        Assert.Empty(CapParser.Active(next,now.AddHours(1)));
    }
    [Fact] public void MissingFromPartialFeedDoesNotCancel()
    {
        var a=CapParser.Parse(Xml,"test");
        Assert.Single(CapParser.Active(CapParser.Merge([a],[],a.Sent.AddHours(1)),a.Sent.AddHours(1)));
    }
    [Fact] public void FutureTripDoesNotInheritTodaysWarning()
    {
        var a=CapParser.Parse(Xml,"test");
        var i=DomainTests.Input() with { DepartureAt=a.To!.Value.AddDays(3) };
        Assert.Empty(WeatherEngine.Match(i,DomainTests.Route() with { Synthetic=false },[a]));
    }
    [Fact] public void RealWarningNeverAttachesToSyntheticRoute()
    { Assert.Empty(WeatherEngine.Match(DomainTests.Input(),DomainTests.Route(),[CapParser.Parse(Xml,"test")])); }
    [Fact] public void SevereMatchedAlertRemainsVisibleWithRoadCoverageMissing()
    {
        var a=CapParser.Parse(Xml,"test");
        var input=DomainTests.Input() with { DepartureAt=a.From };
        var assessment=AssessmentEngine.Build(Guid.NewGuid(),1,input,DomainTests.Route() with { Synthetic=false },a.Sent);
        var result=WeatherEngine.Apply(assessment,[a],new("weather","partial","CAP test"));
        Assert.Equal("adverse",result.Status); Assert.NotEmpty(result.WeatherAlerts);
        Assert.Contains(result.Coverage,c=>c.Dimension=="roads" && c.Availability=="unavailable");
    }
}
