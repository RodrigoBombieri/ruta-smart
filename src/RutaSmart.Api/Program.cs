using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Npgsql;
using RutaSmart.Core;
using RutaSmart.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 65536);
builder.Services.AddProblemDetails();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(new SemaphoreSlim(1, 1));
var connectionString = builder.Configuration.GetConnectionString("Database");
if (string.IsNullOrWhiteSpace(connectionString))
{
    if (!builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing"))
        throw new InvalidOperationException("Configure ConnectionStrings__Database. Memory storage is only allowed in Development/Testing.");
    builder.Services.AddSingleton<IStore, MemoryStore>();
}
else
{
    builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));
    builder.Services.AddSingleton<IStore, PostgresStore>();
}
var routingEnabled = builder.Configuration.GetValue<bool>("Routing:Enabled") && builder.Configuration.GetValue<bool>("Routing:PersistenceApproved");
builder.Services.AddSingleton(new RoutingOptions(routingEnabled, builder.Configuration["Routing:ApiKey"] ?? "",
    builder.Configuration["Routing:CoverageWkt"] ?? "", builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"),
    builder.Configuration["Routing:Provider"] ?? "ors"));
builder.Services.AddHttpClient<RoutingProvider>(c => {
    c.Timeout = TimeSpan.FromSeconds(12);
    c.DefaultRequestHeaders.UserAgent.ParseAdd("RutaSmart/0.1 (trip-planner)");
})
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddHttpClient<LocationProvider>(c => c.Timeout = TimeSpan.FromSeconds(8))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.None);
builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);
builder.Services.AddHttpClient("smn",c => c.Timeout=TimeSpan.FromSeconds(10))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect=false });
builder.Services.AddSingleton(s => new SmnCapProvider(s.GetRequiredService<IHttpClientFactory>().CreateClient("smn"),s.GetRequiredService<IStore>()));
if (!builder.Environment.IsEnvironment("Testing")) builder.Services.AddHostedService<SmnWorker>();
builder.Services.AddHttpClient("energy",c=>c.Timeout=TimeSpan.FromSeconds(30)).ConfigurePrimaryHttpMessageHandler(()=>new HttpClientHandler { AllowAutoRedirect=false });
builder.Services.AddSingleton(new EnergyOptions(builder.Configuration["Energy:CsvPath"],builder.Configuration.GetValue<bool?>("Energy:AllowPublicHttp") ?? builder.Environment.IsDevelopment()));
builder.Services.AddSingleton(s=>new EnergyProvider(s.GetRequiredService<IHttpClientFactory>().CreateClient("energy"),s.GetRequiredService<IStore>(),s.GetRequiredService<EnergyOptions>()));
if (!builder.Environment.IsEnvironment("Testing")) builder.Services.AddHostedService<EnergyWorker>();
builder.Services.AddHostedService<RetentionWorker>();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(
    builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5173", "http://127.0.0.1:5173"])
    .AllowAnyHeader().WithMethods("GET", "POST", "DELETE")));
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "3600";
        await Results.Problem(statusCode: 429, title: "Límite temporal alcanzado. Intentá más tarde.",
            extensions: new Dictionary<string, object?> { ["code"] = "rate_limited" }).ExecuteAsync(context.HttpContext);
    };
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ => RateLimitPartition.GetFixedWindowLimiter("server",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 240, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    o.AddPolicy("analysis", c => RateLimitPartition.GetFixedWindowLimiter(c.Items["owner"]?.ToString() ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromHours(1), QueueLimit = 0 }));
    o.AddPolicy("session", c => RateLimitPartition.GetFixedWindowLimiter(c.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromHours(1), QueueLimit = 0 }));
});
var app = builder.Build();
app.Use(async (context, next) =>
{
    context.Response.Headers.CacheControl = "no-store";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    try { await next(); }
    catch (Exception e) when (!context.Response.HasStarted)
    {
        var domain = e as DomainException;
        var status = domain?.Status ?? (e is JsonException or BadHttpRequestException ? 400 : e is HttpRequestException or OperationCanceledException or NpgsqlException ? 503 : 500);
        await Results.Problem(statusCode: status, title: domain?.Message ?? (status == 400 ? "Solicitud inválida." : "No pudimos completar la operación. Intentá más tarde."),
            extensions: new Dictionary<string, object?> { ["code"] = domain?.Code ?? "request_failed", ["correlationId"] = context.TraceIdentifier }).ExecuteAsync(context);
    }
});
app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";
    if (path.StartsWith("/api/v1/") && path is not ("/api/v1/sessions/anonymous" or "/api/v1/sources/status"))
    {
        var auth = context.Request.Headers.Authorization.ToString();
        if (!auth.StartsWith("Bearer ") || auth.Length != 71) throw new DomainException("unauthorized", "La sesión venció. Volvé a conectarte.", 401);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(auth[7..])));
        if (!await context.RequestServices.GetRequiredService<IStore>().HasSession(hash, DateTimeOffset.UtcNow, context.RequestAborted))
            throw new DomainException("unauthorized", "La sesión venció. Volvé a conectarte.", 401);
        context.Items["owner"] = hash;
    }
    await next();
});
app.UseRateLimiter();
app.MapGet("/health/live", () => Results.Ok(new { status = "alive" }));
app.MapGet("/api/v1/sources/status", (SmnCapProvider smn,EnergyProvider energy) => new {
    routing = routingEnabled ? "configured_pending_benchmark" : app.Environment.IsDevelopment() && builder.Configuration["Routing:Provider"]=="osrm_public" ? "osrm_public_pilot" : "not_configured",
    weather = smn.Coverage(DateTimeOffset.UtcNow).Availability,
    weatherDetail = smn.Coverage(DateTimeOffset.UtcNow).Message,
    weatherLastSuccessAt = smn.Snapshot.LastSuccessAt,
    weatherRecords = CapParser.Active(smn.Snapshot.Alerts,DateTimeOffset.UtcNow).Length,
    weatherRejected = smn.Snapshot.Rejected,
    roads = "unavailable", stations = energy.Coverage(DateTimeOffset.UtcNow).Availability,
    stationsDetail = energy.Coverage(DateTimeOffset.UtcNow).Message,
    stationsCount = energy.Snapshot?.Stations.Select(s=>s.Id).Distinct().Count() ?? 0,
    stationsUpdatedAt = energy.Snapshot?.FetchedAt,
    prices = "dated_station_references_and_manual_trip_cost",
    accounts = "not_configured", storage = connectionString is null ? "ephemeral" : "postgresql",
    demoAvailable = app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing") });
app.MapPost("/api/v1/sessions/anonymous", async (IStore store, TimeProvider clock, CancellationToken ct) =>
{
    var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    var expiry = clock.GetUtcNow().AddHours(24);
    await store.AddSession(new(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))), expiry), ct);
    return Results.Json(new { token, expiresAt = expiry }, statusCode: 201);
}).RequireRateLimiting("session");
app.MapGet("/api/v1/locations/search", async (string q, LocationProvider provider, CancellationToken ct) => await provider.Search(q.Trim(), ct));
app.MapGet("/api/v1/trips/{id:guid}", async (Guid id, HttpContext context, IStore store, CancellationToken ct) =>
    await store.Get(Owner(context), id, DateTimeOffset.UtcNow, ct) is { } trip ? Results.Ok(trip) : Results.NotFound());
app.MapDelete("/api/v1/trips/{id:guid}", async (Guid id, HttpContext context, IStore store, SemaphoreSlim gate, CancellationToken ct) =>
{
    await gate.WaitAsync(ct);
    try
    {
        if (await store.Get(Owner(context), id, DateTimeOffset.UtcNow, ct) is null) return Results.NotFound();
        await store.Delete(Owner(context), id, ct); return Results.NoContent();
    }
    finally { gate.Release(); }
});
app.MapPost("/api/v1/trips/analyze", (JsonElement body, HttpContext c, IStore s, RoutingProvider r, SemaphoreSlim gate, TimeProvider clock, SmnCapProvider smn) => Analyze(null, body,c,s,r,gate,clock,smn))
    .RequireRateLimiting("analysis");
app.MapPost("/api/v1/trips/{id:guid}/analyses", (Guid id, JsonElement body, HttpContext c, IStore s, RoutingProvider r, SemaphoreSlim gate, TimeProvider clock, SmnCapProvider smn) => Analyze(id,body,c,s,r,gate,clock,smn))
    .RequireRateLimiting("analysis");
app.MapGet("/api/v1/trips", () => Results.Problem(statusCode: 403, title: "Guardado anónimo disponible en este dispositivo; cuentas no habilitadas."));
app.MapFallbackToFile("index.html");
app.Run();

static string Owner(HttpContext c) => (string)c.Items["owner"]!;
static async Task<IResult> Analyze(Guid? id, JsonElement body, HttpContext c, IStore store, RoutingProvider provider, SemaphoreSlim gate, TimeProvider clock, SmnCapProvider smn)
{
    using var deadline = CancellationTokenSource.CreateLinkedTokenSource(c.RequestAborted);
    deadline.CancelAfter(TimeSpan.FromSeconds(15));
    var ct = deadline.Token;
    var key = c.Request.Headers["Idempotency-Key"].ToString();
    if (!Regex.IsMatch(key, "^[a-zA-Z0-9_-]{8,100}$")) throw new DomainException("invalid_idempotency_key", "Idempotency-Key es obligatorio (8–100 caracteres).", 400);
    key = (id?.ToString() ?? "new") + ":" + key;
    var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body.GetRawText())));
    var owner = Owner(c);
    await gate.WaitAsync(ct);
    try
    {
        var now = clock.GetUtcNow();
        var replay = await store.GetReplay(owner,key,now,ct);
        if (replay is not null)
        {
            if (replay.Hash != hash) throw new DomainException("idempotency_conflict", "La clave ya fue usada para otra solicitud.", 409);
            return Results.Json(replay.Assessment, statusCode: 201);
        }
        if (body.ValueKind != JsonValueKind.Object || !body.TryGetProperty("departureAt", out var departure) || departure.ValueKind != JsonValueKind.String ||
            !Regex.IsMatch(departure.GetString()!, @"(Z|[+-]\d{2}:\d{2})$"))
            throw new DomainException("offset_required", "La fecha de salida debe incluir un offset explícito.", 400);
        var input = body.Deserialize<TripInput>(new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? throw new JsonException();
        InputValidator.Validate(input,now);
        var previous = id.HasValue ? await store.Get(owner,id.Value,now,ct) : null;
        if (id.HasValue && previous is null) return Results.NotFound();
        var route = await provider.Route(input,ct);
        var assessment = AssessmentEngine.Build(id ?? Guid.NewGuid(), (previous?.Version ?? 0)+1,input,route,now);
        if (!route.Synthetic) assessment = WeatherEngine.Apply(assessment,CapParser.Active(smn.Snapshot.Alerts,now),smn.Coverage(now));
        assessment=c.RequestServices.GetRequiredService<EnergyProvider>().Apply(assessment,now);
        await store.Commit(owner,key,hash,assessment,now.AddHours(24),ct);
        return Results.Created($"/api/v1/trips/{assessment.TripId}",assessment);
    }
    finally { gate.Release(); }
}

public partial class Program { }
sealed class EnergyWorker(EnergyProvider provider,ILogger<EnergyWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await provider.Restore(stoppingToken); }
        catch (Exception) when (!stoppingToken.IsCancellationRequested) { logger.LogWarning("Energy snapshot restore unavailable."); }
        using var timer=new PeriodicTimer(TimeSpan.FromHours(6));
        do
        {
            try { await provider.Refresh(stoppingToken); }
            catch (Exception) when (!stoppingToken.IsCancellationRequested) { logger.LogWarning("Energy refresh failed. Previous snapshot retained."); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
sealed class SmnWorker(SmnCapProvider provider,ILogger<SmnWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await provider.Restore(stoppingToken); }
        catch (Exception) when (!stoppingToken.IsCancellationRequested) { logger.LogWarning("SMN snapshot restore unavailable."); }
        using var timer=new PeriodicTimer(TimeSpan.FromMinutes(5));
        do
        {
            try
            {
                using var deadline=CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                deadline.CancelAfter(TimeSpan.FromSeconds(60));
                await provider.Refresh(deadline.Token);
            }
            catch (Exception) when (!stoppingToken.IsCancellationRequested) { logger.LogWarning("SMN refresh failed. Previous snapshot retained with original timestamps."); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
sealed class RetentionWorker(IStore store, ILogger<RetentionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        do
        {
            try { await store.Purge(DateTimeOffset.UtcNow,stoppingToken); }
            catch (Exception) when (!stoppingToken.IsCancellationRequested) { logger.LogError("Retention cleanup failed; check database availability."); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
