using Akka.Cluster.Hosting;
using Akka.Hosting;
using Akka.Pathfinder.AcceptanceTests.Containers;
using Akka.Pathfinder.Core;
using Akka.Pathfinder.Core.Messages;
using Akka.Remote.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Akka.Pathfinder.AcceptanceTests.Drivers;

public class AkkaDriver : Hosting.TestKit.TestKit
{
    protected const int Port = 4200;
    protected const string Hostname = "127.0.0.1";
    private readonly string _actorSystemName;

    public AkkaDriver(string actorSystemName = "Zeus") : base(actorSystemName,
        startupTimeout: TimeSpan.FromSeconds(15))
    {
        Serilog.Log.Information("[TEST][{AkkaDriverName}][{ActorSystemName}] ctor", GetType().Name, actorSystemName);
        _actorSystemName = actorSystemName;
    }

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        Serilog.Log.Information("[TEST][{AkkaDriverName}][{ActorSystemName}] ConfigureAkka", GetType().Name,
            _actorSystemName);

        builder
            .WithRemoting("0.0.0.0", Port, Hostname)
            .WithClustering(new ClusterOptions
            {
                Roles = ["LULW"],
                SeedNodes =
                [
                    $"akka.tcp://{_actorSystemName}@{LighthouseNodeContainer.Hostname}:{LighthouseNodeContainer.Port}"
                ]
            });

        ConfigureAkkaServices(builder);
    }

    protected static void ConfigureAkkaServices(AkkaConfigurationBuilder builder)
    {
        builder
            .WithShardRegionProxy<PathfinderProxy>("PathfinderWorker", "KEKW", new MessageExtractor())
            .WithSingletonProxy<MapManagerProxy>("MapManager", new ClusterSingletonOptions() { Role = "KEKW" });
    }

    public void TellPathfinder(object request)
    {
        var pathfinderClient = Host.Services.GetRequiredService<IActorRegistry>().Get<PathfinderProxy>();
        pathfinderClient.Tell(request, TestActor);
    }

    public void TellMapManager(object request)
    {
        var mapManagerClient = Host.Services.GetRequiredService<IActorRegistry>().Get<MapManagerProxy>();
        mapManagerClient.Tell(request, TestActor);
    }

    public T Expect<T>(int seconds) => ExpectMsg<T>(TimeSpan.FromSeconds(seconds));

    public PathfinderResponse ReceivePathFound(Guid PathfinderId) => FishForMessage<PathfinderResponse>(msg => msg.PathfinderId == PathfinderId, TimeSpan.FromSeconds(180));
}