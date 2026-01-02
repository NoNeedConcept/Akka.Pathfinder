using Akka.Cluster.Hosting;
using Akka.Hosting;
using Akka.Logger.Serilog;
using Akka.Pathfinder.Core;
using Akka.Pathfinder.Managers;
using Akka.Pathfinder.Workers;
using Akka.Persistence.Hosting;
using Akka.Persistence.MongoDb.Hosting;
using Akka.Remote.Hosting;
using moin.akka.endpoint;
using Servus.Akka.Startup;

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
        var connectionString = configuration.GetConnectionString("mongodb");
        var remoteSection = configuration.GetSection("akka:remote:dot-netty:tcp");
        var remoteOptions = new RemoteOptions
        {
            HostName = "0.0.0.0",
            Port = remoteSection.GetSection("port").Get<int?>(),
            PublicHostName = remoteSection.GetSection("public-hostname").Get<string>(),
        };
        var clusterSection = configuration.GetSection("akka:cluster");
        var clusterOptions = new ClusterOptions
        {
            Roles = ["Pathfinder"],
            SeedNodes = clusterSection.GetSection("seed-nodes").Get<string[]>(),
        };


        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        var shardingJournalOptions = new MongoDbJournalOptions(true)
        {
            ConnectionString = connectionString,
            Collection = "events",
            MetadataCollection = "metadatas",
            UseWriteTransaction = false,
            UseReadTransaction = false,
            AutoInitialize = true,
        };

        var shardingSnapshotOptions = new MongoDbSnapshotOptions(true)
        {
            ConnectionString = connectionString,
            Collection = "snapshots",
            UseWriteTransaction = false,
            UseReadTransaction = false,
            AutoInitialize = true,
        };

        builder
            .ConfigureLoggers(setup =>
            {
                // Clear all loggers
                setup.ClearLoggers();

                // Add serilog logger
                setup.AddLogger<SerilogLogger>();
                setup.WithDefaultLogMessageFormatter<SerilogLogMessageFormatter>();
            })
            .AddHocon(hocon: "akka.remote.dot-netty.tcp.maximum-frame-size = 256000b", addMode: HoconAddMode.Prepend)
            .AddHocon(@"
akka.actor.dispatchers.entity-dispatcher {
  type = Dispatcher
  executor = fork-join-executor

  fork-join-executor {
    parallelism-min = 4
    parallelism-max = 16
    parallelism-factor = 2.0
  }

  throughput = 20
}
", HoconAddMode.Prepend)
            .WithActorSystemLivenessCheck()
            .WithAkkaClusterReadinessCheck()
            .WithMongoDbPersistence(string.Empty,
                journalBuilder: journal => journal.WithHealthCheck(),
                snapshotBuilder: snapshot => snapshot.WithHealthCheck())
            .WithRemoting(remoteOptions)
            .WithClustering(clusterOptions)
            .WithMongoDbPersistence(connectionString)
            .WithJournalAndSnapshot(shardingJournalOptions, shardingSnapshotOptions)
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
                        .WithDispatcher("akka.actor.dispatchers.entity-dispatcher");
                },
                new MessageExtractor(25),
                new ShardOptions
                {
                    Role = "Pathfinder",
                    RememberEntities = false
                });
    }
}