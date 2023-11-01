using Akka.Cluster.Hosting;
using Akka.HealthCheck.Hosting;
using Akka.HealthCheck.Hosting.Web;
using Akka.Hosting;
using Akka.Pathfinder;
using Akka.Pathfinder.Core;
using Akka.Pathfinder.Core.Configs;
using Akka.Pathfinder.Core.Services;
using Akka.Persistence.Sql.Config;
using Akka.Persistence.Sql.Hosting;
using Akka.Remote.Hosting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using MongoDB.Driver;
using Serilog;
using Path = Akka.Pathfinder.Core.Persistence.Data.Path;

Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.Debug()
            .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog(Log.Logger);

RegisterMongoShit();

builder.Services.AddHealthChecks();

builder.Services.WithAkkaHealthCheck(HealthCheckType.Cluster)
.AddSingleton<IMongoClient>(x => new MongoClient(AkkaPathfinder.GetEnvironmentVariable("mongodb")))
.AddScoped(x => x.GetRequiredService<IMongoClient>().GetDatabase("pathfinder"))
.AddScoped(x => x.GetRequiredService<IMongoDatabase>().GetCollection<Path>("path"))
.AddScoped(x => x.GetRequiredService<IMongoDatabase>().GetCollection<PointConfig>("point_config"))
.AddScoped<IPathWriter, PathWriter>()
.AddScoped<IPathReader>(x => x.GetRequiredService<IPathWriter>())
.AddScoped<IPointConfigReader, PointConfigReader>()
.AddAkka("Zeus", (builder, sp) =>
    {
        var connectionString = AkkaPathfinder.GetEnvironmentVariable("postgre");
        var shardingJournalOptions = new SqlJournalOptions(true)
        {
            ConnectionString = connectionString,
            ProviderName = LinqToDB.ProviderName.PostgreSQL15,
            TagStorageMode = TagMode.TagTable,
            AutoInitialize = true
        };

        var shardingSnapshotOptions = new SqlSnapshotOptions(true)
        {
            ConnectionString = connectionString,
            ProviderName = LinqToDB.ProviderName.PostgreSQL15,
            AutoInitialize = true
        };

        builder
            .WithHealthCheck(x =>
            {
                x.AddProviders(HealthCheckType.Cluster);
                x.Liveness.PersistenceProbeInterval = TimeSpan.FromSeconds(5);
                x.Readiness.PersistenceProbeInterval = TimeSpan.FromSeconds(5);
                x.LogConfigOnStart = true;
            })
            .WithWebHealthCheck(sp)
            .WithRemoting("0.0.0.0", 1337, "127.0.0.1")
            .WithClustering(new ClusterOptions
            {
                Roles = new[] { "KEKW" },
                SeedNodes = new[] { "akka.tcp://Zeus@127.0.0.1:42000" }
            })
            .WithSqlPersistence(shardingJournalOptions, shardingSnapshotOptions)
            .WithShardRegion<PointWorker>("PointWorker", (_, _, dependecyResolver) => x => dependecyResolver.Props<PointWorker>(x), new MessageExtractor(), new ShardOptions()
            {
                JournalOptions = shardingJournalOptions,
                SnapshotOptions = shardingSnapshotOptions,
                Role = "KEKW",
                PassivateIdleEntityAfter = null,
            })
            .WithShardRegionProxy<PointWorkerProxy>("PointWorker", "KEKW", new MessageExtractor())
            .WithShardRegion<PathfinderWorker>("PathfinderWorker", (_, _, dependecyResolver) => x => dependecyResolver.Props<PathfinderWorker>(x), new MessageExtractor(), new ShardOptions()
            {
                JournalOptions = shardingJournalOptions,
                SnapshotOptions = shardingSnapshotOptions,
                Role = "KEKW",
                PassivateIdleEntityAfter = null,
            })
            .WithShardRegionProxy<PathfinderProxy>("PathfinderWorker", "KEKW", new MessageExtractor());
    });


var host = builder.Build();
host.UseHealthChecks("/health/ready", new HealthCheckOptions() { AllowCachingResponses = false, Predicate = _ => true });
await host.RunAsync().ConfigureAwait(false);

public partial class Program
{
    private static int _registered;
    protected Program()
    {
    }

    private static void RegisterMongoShit()
    {
        if (Interlocked.Increment(ref _registered) == 1)
            BsonShit.Register();
    }
}

namespace Akka.Pathfinder
{
    public record PointWorkerProxy;

    public record PathfinderProxy;
}
