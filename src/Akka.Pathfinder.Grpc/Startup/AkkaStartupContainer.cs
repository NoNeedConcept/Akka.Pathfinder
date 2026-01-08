using Akka.Cluster.Hosting;
using Akka.Hosting;
using Akka.Logger.Serilog;
using Akka.Pathfinder.Core;
using Akka.Pathfinder.Grpc.Forwarder;
using Akka.Remote.Hosting;
using Servus.Akka.Startup;
using moin.akka.endpoint;
using Servus.Akka;
using Endpoint = Akka.Pathfinder.Core.Endpoint;

namespace Akka.Pathfinder.Grpc.Startup;

public class AkkaStartupContainer : ActorSystemSetupContainer
{
    protected override string GetActorSystemName()
    {
        return "zeus";
    }

    protected override void BuildSystem(AkkaConfigurationBuilder builder, IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
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
            Roles = ["Pathfinder.Grpc"],
            SeedNodes = clusterSection.GetSection("seed-nodes").Get<string[]>()
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
            .WithActorSystemLivenessCheck()
            .WithAkkaClusterReadinessCheck()
            .WithRemoting(remoteOptions)
            .WithClustering(clusterOptions)
            .AddClient<Endpoint.MapManager>()
            .AddClient<Endpoint.SenderManager>()
            .AddClient<Endpoint.PointWorker>(new MessageExtractor(25))
            .AddClient<Endpoint.PathfinderWorker>(new MessageExtractor(10))
            .WithActors((system, registry) =>
            {
                registry.Register<RequestForwarder>(system.ResolveActor<RequestForwarder>());
            });
    }
}