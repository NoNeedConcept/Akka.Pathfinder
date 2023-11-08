using Akka.Cluster.Hosting;
using Akka.Hosting;
using Akka.Pathfinder.AcceptanceTests.Containers;
using Akka.Pathfinder.Core;
using Akka.Pathfinder.Core.Configs;
using Akka.Pathfinder.Core.Messages;
using Akka.Pathfinder.Core.Services;
using Akka.Remote.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

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
                Roles = new[] { "LULW" },
                SeedNodes = new[]
                {
                    $"akka.tcp://{_actorSystemName}@{LighthouseNodeContainer.Hostname}:{LighthouseNodeContainer.Port}"
                }
            });

        ConfigureAkkaServices(builder);
    }

    protected static void ConfigureAkkaServices(AkkaConfigurationBuilder builder)
    {
        builder
            .WithShardRegionProxy<PathfinderProxy>("PathfinderWorker", "KEKW", new MessageExtractor())
            .WithShardRegionProxy<PointWorkerProxy>("PointWorker", "KEKW", new MessageExtractor())
            .WithSingletonProxy<MapManagerProxy>("MapManager", new ClusterSingletonOptions() { Role = "KEKW" });
    }

    protected override void ConfigureServices(HostBuilderContext _, IServiceCollection services)
    {
        services
        .AddSingleton<IMongoClient>(x => new MongoClient(AkkaPathfinder.GetEnvironmentVariable("mongodb")))
        .AddScoped(x => x.GetRequiredService<IMongoClient>().GetDatabase("pathfinder"))
        .AddScoped(x => x.GetRequiredService<IMongoDatabase>().GetCollection<Core.Persistence.Data.Path>("path"))
        .AddScoped(x => x.GetRequiredService<IMongoDatabase>().GetCollection<MapConfig>("map_config"))
        .AddScoped<IPathWriter, PathWriter>()
        .AddScoped<IPathReader>(x => x.GetRequiredService<IPathWriter>())
        .AddScoped<IMapConfigWriter, MapConfigWriter>()
        .AddScoped<IMapConfigReader>(x => x.GetRequiredService<IMapConfigWriter>())
        .AddScoped<IPointConfigWriter, PointConfigWriter>()
        .AddScoped<IPointConfigReader>(x => x.GetRequiredService<IPointConfigWriter>());
    }

    public void TellPathfinder(object request)
    {
        var pathfinderClient = Host.Services.GetRequiredService<IActorRegistry>().Get<PathfinderProxy>();
        pathfinderClient.Tell(request, TestActor);
    }

    public void TellPointWorker(object request)
    {
        var pointWorkerClient = Host.Services.GetRequiredService<IActorRegistry>().Get<PointWorkerProxy>();
        pointWorkerClient.Tell(request, TestActor);
    }

    public void TellMapManager(object request)
    {
        var mapManagerClient = Host.Services.GetRequiredService<IActorRegistry>().Get<MapManagerProxy>();
        mapManagerClient.Tell(request, TestActor);
        
    }

    public T Expect<T>(int seconds) => ExpectMsg<T>(TimeSpan.FromSeconds(seconds));

    public PathFinderDone ReceivePathFound() => ExpectMsg<PathFinderDone>(TimeSpan.FromSeconds(180));
}