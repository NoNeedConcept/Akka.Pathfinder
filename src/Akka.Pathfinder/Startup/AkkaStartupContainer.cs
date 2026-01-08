using Akka.Cluster.Hosting;
using Akka.Hosting;
using Akka.Logger.Serilog;
using Akka.Pathfinder.Core;
using Akka.Pathfinder.Managers;
using Akka.Pathfinder.Workers;
using Akka.Persistence.Redis.Hosting;
using Akka.Persistence.MongoDb.Hosting;
using Akka.Remote.Hosting;
using moin.akka.endpoint;
using Serilog;
using Servus.Akka.Startup;
using Endpoint = Akka.Pathfinder.Core.Endpoint;

namespace Akka.Pathfinder.Startup;

public class AkkaStartupContainer : ActorSystemSetupContainer
{
    protected override string GetActorSystemName()
    {
        return "zeus";
    }

    protected override void BuildSystem(AkkaConfigurationBuilder builder, IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var redisConnectionString = configuration.GetConnectionString("redis");
        var mongodbConnectionString = configuration.GetConnectionString("mongodb");
        var remoteSection = configuration.GetSection("akka:remote:dot-netty:tcp");
        var remoteOptions = new RemoteOptions
        {
            HostName = "0.0.0.0",
            Port = remoteSection.GetSection("port").Get<int?>(),
            PublicHostName = remoteSection.GetSection("public-hostname").Get<string>()
        };
        var clusterSection = configuration.GetSection("akka:cluster");
        var clusterOptions = new ClusterOptions
        {
            Roles = ["Pathfinder"],
            SeedNodes = clusterSection.GetSection("seed-nodes").Get<string[]>()
        };

        var useTransactions = mongodbConnectionString?.Contains("replicaSet=") ?? false;

        var shardingJournalOptions = new MongoDbJournalOptions(true)
        {
            ConnectionString = mongodbConnectionString!,
            Collection = "events",
            MetadataCollection = "metadatas",
            UseWriteTransaction = useTransactions,
            UseReadTransaction = useTransactions,
            AutoInitialize = true
        };

        var shardingSnapshotOptions = new MongoDbSnapshotOptions(true)
        {
            ConnectionString = mongodbConnectionString!,
            Collection = "snapshots",
            UseWriteTransaction = useTransactions,
            UseReadTransaction = useTransactions,
            AutoInitialize = true
        };

        builder
            .ConfigureLoggers(setup =>
            {
                // Clear all loggers
                setup.ClearLoggers();

                // Add serilog logger
                setup.AddLogger<SerilogLogger>();
                setup.DebugOptions = new DebugOptions
                {
                    Unhandled = true,
                    LifeCycle = true
                };
                setup.DeadLetterOptions = new DeadLetterOptions
                {
                    ShouldLog = TriStateValue.All
                };
            })
            .AddHocon(hocon: "akka.remote.dot-netty.tcp.maximum-frame-size = 256000b", addMode: HoconAddMode.Prepend)
            .AddHocon(hocon: @"akka.actor.dispatchers.pointworker-dispatcher {
  type = Dispatcher
  executor = fork-join-executor

  fork-join-executor {
    parallelism-min = 3
    parallelism-max = 12
    parallelism-factor = 1.0
  }

  throughput = 5
}", HoconAddMode.Prepend)
            .WithActorSystemLivenessCheck()
            .WithAkkaClusterReadinessCheck()
            .WithRemoting(remoteOptions)
            .WithClustering(clusterOptions)
            .AddService<MapManager, Endpoint.MapManager>()
            .AddClient<Endpoint.MapManager>()
            .AddService<SenderManager, Endpoint.SenderManager>()
            .AddClient<Endpoint.SenderManager>()
            //.AddService<PointWorker, Endpoint.PointWorker>(new MessageExtractor())
            .AddClient<Endpoint.PointWorker>(new MessageExtractor(25))
            .AddService<PathfinderWorker, Endpoint.PathfinderWorker>(new MessageExtractor(10))
            .AddClient<Endpoint.PathfinderWorker>(new MessageExtractor(10))
            .WithShardRegion<PointWorker>("Pathfinder-Point",
                entityPropsFactory: (_, _, resolver) =>
                {
                    return nttId => resolver.Props<PointWorker>(nttId)
                        .WithDispatcher("akka.actor.dispatchers.pointworker-dispatcher");
                },
                new MessageExtractor(25),
                new ShardOptions
                {
                    Role = "Pathfinder",
                    PassivateIdleEntityAfter = TimeSpan.FromMinutes(1),
                });

        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            Log.Information("Use Redis for akka persistence");
            builder.WithRedisPersistence(redisConnectionString,
                journalBuilder: journalBuilder => journalBuilder.WithHealthCheck(),
                snapshotBuilder: snapshotBuilder => snapshotBuilder.WithHealthCheck());
        }
        else
        {
            Log.Information("Use MongoDb for akka persistence");
            builder
                .WithMongoDbPersistence(
                    journalOptions: shardingJournalOptions,
                    snapshotOptions: shardingSnapshotOptions,
                    journalBuilder: journal => journal.WithHealthCheck(),
                    snapshotBuilder: snapshot => snapshot.WithHealthCheck());
        }
    }
}