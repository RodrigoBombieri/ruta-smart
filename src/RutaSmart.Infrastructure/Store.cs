using System.Collections.Concurrent;
using System.Text.Json;
using Npgsql;
using RutaSmart.Core;

namespace RutaSmart.Infrastructure;

public sealed record SessionRecord(string Hash, DateTimeOffset ExpiresAt);
public sealed record StoredTrip(string Owner, Assessment Assessment, DateTimeOffset ExpiresAt);
public sealed record Replay(string Hash, Assessment Assessment);
public interface IStore
{
    Task AddSession(SessionRecord session, CancellationToken ct);
    Task<bool> HasSession(string hash, DateTimeOffset now, CancellationToken ct);
    Task<Assessment?> Get(string owner, Guid id, DateTimeOffset now, CancellationToken ct);
    Task<Replay?> GetReplay(string owner, string key, DateTimeOffset now, CancellationToken ct);
    Task Commit(string owner, string key, string hash, Assessment assessment, DateTimeOffset expiry, CancellationToken ct);
    Task Delete(string owner, Guid id, CancellationToken ct);
    Task Purge(DateTimeOffset now, CancellationToken ct);
    Task<string?> ReadSource(string name, CancellationToken ct);
    Task WriteSource(string name, string json, CancellationToken ct);
}

// Explicitly limited to local development and tests; snapshots disappear on restart.
public sealed class MemoryStore : IStore
{
    readonly ConcurrentDictionary<string,string> sources = new();
    public Task<string?> ReadSource(string name,CancellationToken ct) => Task.FromResult(sources.GetValueOrDefault(name));
    public Task WriteSource(string name,string json,CancellationToken ct) { sources[name]=json; return Task.CompletedTask; }
    readonly ConcurrentDictionary<string, SessionRecord> sessions = new();
    readonly ConcurrentDictionary<(string, Guid), StoredTrip> trips = new();
    readonly ConcurrentDictionary<(string, string), (Replay Replay, DateTimeOffset Expiry)> replays = new();
    public Task AddSession(SessionRecord s, CancellationToken ct) { sessions[s.Hash] = s; return Task.CompletedTask; }
    public Task<bool> HasSession(string hash, DateTimeOffset now, CancellationToken ct) => Task.FromResult(sessions.TryGetValue(hash, out var s) && s.ExpiresAt > now);
    public Task<Assessment?> Get(string owner, Guid id, DateTimeOffset now, CancellationToken ct) => Task.FromResult(trips.TryGetValue((owner,id), out var t) && t.ExpiresAt > now ? t.Assessment : null);
    public Task<Replay?> GetReplay(string owner, string key, DateTimeOffset now, CancellationToken ct) => Task.FromResult(replays.TryGetValue((owner,key), out var r) && r.Expiry > now ? r.Replay : null);
    public Task Commit(string owner, string key, string hash, Assessment a, DateTimeOffset expiry, CancellationToken ct)
    { trips[(owner,a.TripId)] = new(owner,a,expiry); replays[(owner,key)] = (new(hash,a),expiry); return Task.CompletedTask; }
    public Task Delete(string owner, Guid id, CancellationToken ct)
    {
        trips.TryRemove((owner,id), out _);
        foreach (var pair in replays.Where(p => p.Key.Item1 == owner && p.Value.Replay.Assessment.TripId == id)) replays.TryRemove(pair.Key, out _);
        return Task.CompletedTask;
    }
    public Task Purge(DateTimeOffset now, CancellationToken ct)
    {
        foreach (var p in sessions.Where(p => p.Value.ExpiresAt <= now)) sessions.TryRemove(p.Key, out _);
        foreach (var p in trips.Where(p => p.Value.ExpiresAt <= now)) trips.TryRemove(p.Key, out _);
        foreach (var p in replays.Where(p => p.Value.Expiry <= now)) replays.TryRemove(p.Key, out _);
        return Task.CompletedTask;
    }
}

public sealed class PostgresStore(NpgsqlDataSource dataSource) : IStore
{
    public async Task<string?> ReadSource(string name,CancellationToken ct)
    {
        await using var cmd=dataSource.CreateCommand("SELECT snapshot::text FROM source_snapshots WHERE name=$1");
        cmd.Parameters.AddWithValue(name); return await cmd.ExecuteScalarAsync(ct) as string;
    }
    public async Task WriteSource(string name,string json,CancellationToken ct)
    {
        await using var cmd=dataSource.CreateCommand("INSERT INTO source_snapshots(name,snapshot) VALUES ($1,$2::jsonb) ON CONFLICT(name) DO UPDATE SET snapshot=EXCLUDED.snapshot");
        cmd.Parameters.AddWithValue(name); cmd.Parameters.AddWithValue(json); await cmd.ExecuteNonQueryAsync(ct);
    }
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public async Task AddSession(SessionRecord s, CancellationToken ct)
    {
        await using var cmd = dataSource.CreateCommand("INSERT INTO anonymous_sessions(token_hash, expires_at) VALUES ($1,$2)");
        cmd.Parameters.AddWithValue(s.Hash); cmd.Parameters.AddWithValue(s.ExpiresAt.ToUniversalTime());
        await cmd.ExecuteNonQueryAsync(ct);
    }
    public async Task<bool> HasSession(string hash, DateTimeOffset now, CancellationToken ct)
    {
        await using var cmd = dataSource.CreateCommand("SELECT EXISTS(SELECT 1 FROM anonymous_sessions WHERE token_hash=$1 AND expires_at>$2)");
        cmd.Parameters.AddWithValue(hash); cmd.Parameters.AddWithValue(now.ToUniversalTime());
        return (bool)(await cmd.ExecuteScalarAsync(ct))!;
    }
    public async Task<Assessment?> Get(string owner, Guid id, DateTimeOffset now, CancellationToken ct)
    {
        await using var cmd = dataSource.CreateCommand("SELECT snapshot::text FROM assessments WHERE owner_hash=$1 AND trip_id=$2 AND expires_at>$3 ORDER BY version DESC LIMIT 1");
        cmd.Parameters.AddWithValue(owner); cmd.Parameters.AddWithValue(id); cmd.Parameters.AddWithValue(now.ToUniversalTime());
        var json = await cmd.ExecuteScalarAsync(ct) as string;
        return json is null ? null : JsonSerializer.Deserialize<Assessment>(json, Json);
    }
    public async Task<Replay?> GetReplay(string owner, string key, DateTimeOffset now, CancellationToken ct)
    {
        await using var cmd = dataSource.CreateCommand("SELECT i.body_hash,a.snapshot::text FROM idempotency i JOIN assessments a ON a.trip_id=i.trip_id AND a.version=i.version WHERE i.owner_hash=$1 AND i.key=$2 AND a.expires_at>$3");
        cmd.Parameters.AddWithValue(owner); cmd.Parameters.AddWithValue(key); cmd.Parameters.AddWithValue(now.ToUniversalTime());
        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct) ? new(r.GetString(0), JsonSerializer.Deserialize<Assessment>(r.GetString(1), Json)!) : null;
    }
    public async Task Commit(string owner, string key, string hash, Assessment a, DateTimeOffset expiry, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await using var cmd = new NpgsqlCommand("INSERT INTO assessments(trip_id,version,owner_hash,snapshot,expires_at,route) VALUES ($1,$2,$3,$4::jsonb,$5,ST_GeomFromText($6,4326))", connection, transaction);
        cmd.Parameters.AddWithValue(a.TripId); cmd.Parameters.AddWithValue(a.Version); cmd.Parameters.AddWithValue(owner);
        cmd.Parameters.AddWithValue(JsonSerializer.Serialize(a, Json)); cmd.Parameters.AddWithValue(expiry.ToUniversalTime());
        cmd.Parameters.AddWithValue(Geo.Line(a.Route.Geometry).AsText());
        await cmd.ExecuteNonQueryAsync(ct);
        await using var idem = new NpgsqlCommand("INSERT INTO idempotency(owner_hash,key,body_hash,trip_id,version) VALUES ($1,$2,$3,$4,$5)",connection,transaction);
        idem.Parameters.AddWithValue(owner); idem.Parameters.AddWithValue(key); idem.Parameters.AddWithValue(hash);
        idem.Parameters.AddWithValue(a.TripId); idem.Parameters.AddWithValue(a.Version);
        await idem.ExecuteNonQueryAsync(ct); await transaction.CommitAsync(ct);
    }
    public async Task Delete(string owner, Guid id, CancellationToken ct)
    {
        await using var cmd = dataSource.CreateCommand("DELETE FROM assessments WHERE owner_hash=$1 AND trip_id=$2");
        cmd.Parameters.AddWithValue(owner); cmd.Parameters.AddWithValue(id); await cmd.ExecuteNonQueryAsync(ct);
    }
    public async Task Purge(DateTimeOffset now, CancellationToken ct)
    {
        await using var cmd = dataSource.CreateCommand("DELETE FROM assessments WHERE expires_at <= $1");
        cmd.Parameters.AddWithValue(now.ToUniversalTime()); await cmd.ExecuteNonQueryAsync(ct);
        await using var sessions = dataSource.CreateCommand("DELETE FROM anonymous_sessions WHERE expires_at <= $1");
        sessions.Parameters.AddWithValue(now.ToUniversalTime()); await sessions.ExecuteNonQueryAsync(ct);
    }
}
