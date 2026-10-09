using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using RutaSmart.Core;

namespace RutaSmart.Tests;

public class TestApp : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Testing");
}
public class ApiTests
{
    static async Task<HttpClient> Session(TestApp app)
    {
        var client=app.CreateClient();
        var response=await client.PostAsync("/api/v1/sessions/anonymous",null);
        Assert.Equal(HttpStatusCode.Created,response.StatusCode);
        var json=await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",json.GetProperty("token").GetString());
        return client;
    }
    static Task<HttpResponseMessage> Analyze(HttpClient client,TripInput input,string key="test-key-001")
    {
        var request=new HttpRequestMessage(HttpMethod.Post,"/api/v1/trips/analyze") { Content=JsonContent.Create(input) };
        request.Headers.Add("Idempotency-Key",key); return client.SendAsync(request);
    }
    [Fact] public async Task AnonymousAnalyzeRequiresSession()
    {
        await using var app=new TestApp(); using var client=app.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized,(await Analyze(client,DomainTests.Input())).StatusCode);
    }
    [Fact] public async Task CompleteDemoJourneyAndOwnership()
    {
        await using var app=new TestApp(); using var a=await Session(app); using var b=await Session(app);
        var response=await Analyze(a,DomainTests.Input());
        Assert.Equal(HttpStatusCode.Created,response.StatusCode);
        var report=(await response.Content.ReadFromJsonAsync<Assessment>())!;
        Assert.Equal("insufficient_information",report.Status); Assert.True(report.Route.Synthetic);
        Assert.Null(report.Fuel.ConsumptionCostArs);
        var url=$"/api/v1/trips/{report.TripId}";
        Assert.Equal(HttpStatusCode.OK,(await a.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await b.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await b.DeleteAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,(await a.DeleteAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await a.GetAsync(url)).StatusCode);
    }
    [Fact] public async Task IdempotencyReplaysAndRejectsDifferentInput()
    {
        await using var app=new TestApp(); using var client=await Session(app); var input=DomainTests.Input();
        var one=await Analyze(client,input); var two=await Analyze(client,input);
        Assert.Equal(await one.Content.ReadAsStringAsync(),await two.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Conflict,(await Analyze(client,input with { InitialLitres=20 })).StatusCode);
    }
    [Fact] public async Task UnconfiguredRoutingIs503NotFakeRoute()
    {
        await using var app=new TestApp(); using var client=await Session(app);
        var response=await Analyze(client,DomainTests.Input() with { Demo=false });
        Assert.Equal(HttpStatusCode.ServiceUnavailable,response.StatusCode);
        var error=await response.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal("routing_not_configured",error.GetProperty("code").GetString());
    }
    [Fact] public async Task MissingIdempotencyIs400()
    {
        await using var app=new TestApp(); using var client=await Session(app);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/v1/trips/analyze",DomainTests.Input())).StatusCode);
    }
    [Fact] public async Task ReanalysisCreatesVersionAndGetDoesNotRecompute()
    {
        await using var app=new TestApp(); using var client=await Session(app);
        var first=(await (await Analyze(client,DomainTests.Input())).Content.ReadFromJsonAsync<Assessment>())!;
        using var request=new HttpRequestMessage(HttpMethod.Post,$"/api/v1/trips/{first.TripId}/analyses") { Content=JsonContent.Create(first.Input with { InitialLitres=20 }) };
        request.Headers.Add("Idempotency-Key","new-analysis-001");
        var second=(await (await client.SendAsync(request)).Content.ReadFromJsonAsync<Assessment>())!;
        Assert.Equal(2,second.Version); Assert.Equal(first.TripId,second.TripId);
        var saved=(await client.GetFromJsonAsync<Assessment>($"/api/v1/trips/{first.TripId}"))!;
        Assert.Equal(second.ComputedAt,saved.ComputedAt);
    }
}
