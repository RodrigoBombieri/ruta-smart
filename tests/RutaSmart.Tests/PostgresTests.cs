using Npgsql;
using RutaSmart.Core;
using RutaSmart.Infrastructure;

namespace RutaSmart.Tests;
public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute() { if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RUTASMART_TEST_DB"))) Skip="Set RUTASMART_TEST_DB to run against real PostGIS."; }
}
public class PostgresTests
{
    [PostgresFact] public async Task PersistenceOwnershipIdempotencySpatialAndRetention()
    {
        var connection=Environment.GetEnvironmentVariable("RUTASMART_TEST_DB")!;
        var schema="rs_test_"+Guid.NewGuid().ToString("N");
        await using var admin=NpgsqlDataSource.Create(connection);
        await using (var create=admin.CreateCommand($"CREATE EXTENSION IF NOT EXISTS postgis; CREATE SCHEMA {schema}")) await create.ExecuteNonQueryAsync();
        try
        {
            var config=new NpgsqlConnectionStringBuilder(connection) { SearchPath=schema+",public" };
            await using var data=NpgsqlDataSource.Create(config.ConnectionString);
            await using (var migration=data.CreateCommand(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory,"001-initial.sql")))) await migration.ExecuteNonQueryAsync();
            var store=new PostgresStore(data); var now=DateTimeOffset.UtcNow;
            await store.AddSession(new("owner",now.AddHours(24)),default);
            var report=AssessmentEngine.Build(Guid.NewGuid(),1,DomainTests.Input(),DomainTests.Route(),now);
            await store.Commit("owner","key","hash",report,now.AddHours(24),default);
            Assert.Equal(report.TripId,(await store.Get("owner",report.TripId,now,default))!.TripId);
            Assert.Null(await store.Get("other",report.TripId,now,default));
            Assert.Equal("hash",(await store.GetReplay("owner","key",now,default))!.Hash);
            await using (var spatial=data.CreateCommand("SELECT ST_SRID(route)=4326 AND ST_IsValid(route) FROM assessments")) Assert.True((bool)(await spatial.ExecuteScalarAsync())!);
            await store.WriteSource("test","{\"version\":1}",default);
            Assert.Contains("version",(await store.ReadSource("test",default))!);
            await store.Delete("other",report.TripId,default);
            Assert.NotNull(await store.Get("owner",report.TripId,now,default));
            await store.Delete("owner",report.TripId,default);
            Assert.Null(await store.GetReplay("owner","key",now,default));
            await store.Commit("owner","key2","hash",report,now.AddSeconds(-1),default);
            await store.Purge(now,default);
            Assert.Null(await store.Get("owner",report.TripId,now,default));
        }
        finally { await using var drop=admin.CreateCommand($"DROP SCHEMA {schema} CASCADE"); await drop.ExecuteNonQueryAsync(); }
    }
}
