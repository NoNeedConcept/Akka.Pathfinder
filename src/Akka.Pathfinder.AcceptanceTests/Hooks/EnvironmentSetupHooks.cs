using Akka.Pathfinder.AcceptanceTests.Containers;
using Akka.Pathfinder.AcceptanceTests.Drivers;
using Akka.Pathfinder.Core;
using Reqnroll;
using Reqnroll.BoDi;
using Serilog;
using Serilog.Extensions.Hosting;

namespace Akka.Pathfinder.AcceptanceTests.Hooks;

[Binding]
public static class EnvironmentSetupHooks
{
    [BeforeTestRun]
    public static void BeforeTestRun()
    {
        Log.Logger = CreateLogger();
        Log.Information("[TEST][EnvironmentSetupHooks][BeforeTestRun]");
        BsonShit.Register();
    }

    [BeforeFeature]
    public static async Task BeforeFeature(ObjectContainer container, FeatureContext featureContext)
    {
        Log.Logger = CreateLogger();
        Log.Information("[TEST][EnvironmentSetupHooks][BeforeFeature]");
        var isRedisForPersistence = featureContext.FeatureInfo.Tags.Contains("RedisActive");
        var mongoDbContainer = new MongoDbContainer();
        var redisDbContainer = new RedisContainer();
        var seedNodeContainer = new LighthouseNodeContainer();

        var lighthouseTask = seedNodeContainer.InitializeAsync();
        var mongoTask = mongoDbContainer.InitializeAsync();
        var redisTask = redisDbContainer.InitializeAsync();

        await lighthouseTask;
        await mongoTask;
        await redisTask;

        var redisDbString = redisDbContainer.GetConnectionString();
        var mongoDbString = mongoDbContainer.GetConnectionString();
        var seedNodeString = seedNodeContainer.GetSeedNodeString();

        if (isRedisForPersistence)
        {
            Log.Information("[TEST][EnvironmentSetupHooks] - Redis: {ConnectionString}", redisDbString);
            Environment.SetEnvironmentVariable("ConnectionStrings__redis", redisDbString);
        }

        Log.Information("[TEST][EnvironmentSetupHooks] - MongoDb: {ConnectionString}", mongoDbString);
        Environment.SetEnvironmentVariable("ConnectionStrings__mongodb", mongoDbString);
        
        Environment.SetEnvironmentVariable("TESTING", "1");
        Environment.SetEnvironmentVariable("akka__cluster__seed-nodes__0", seedNodeString);
        Environment.SetEnvironmentVariable("akka__remote__dot-netty__tcp__public-hostname", "host.docker.internal");
        var pathfinderApplicationFactory = new PathfinderApplicationFactory();
        var grpcApplicationFactory = new GrpcApplicationFactory();
        var pathfinderTask = pathfinderApplicationFactory.InitializeAsync();
        var grpcTask = grpcApplicationFactory.InitializeAsync();

        await Task.WhenAll(grpcTask, pathfinderTask);

        container.RegisterInstanceAs(mongoDbContainer);
        container.RegisterInstanceAs(redisDbContainer);
        container.RegisterInstanceAs(seedNodeContainer);
        container.RegisterInstanceAs(pathfinderApplicationFactory);
        container.RegisterInstanceAs(grpcApplicationFactory);
    }

    [AfterScenario]
    public static void AfterScenario() => Log.Information("[TEST][EnvironmentSetupHooks][AfterScenario]");

    [AfterFeature]
    public static async Task AfterFeature(ObjectContainer container)
    {
        Log.Information("[TEST][EnvironmentSetupHooks][AfterFeature]");
        var pathfinderApplicationFactory = container.Resolve<PathfinderApplicationFactory>();
        await pathfinderApplicationFactory.DisposeAsync();
        var grpcApplicationFactory = container.Resolve<PathfinderApplicationFactory>();
        await grpcApplicationFactory.DisposeAsync();
        var mongoDbContainer = container.Resolve<MongoDbContainer>();
        await mongoDbContainer.DisposeAsync();
        var redisDbContainer = container.Resolve<RedisContainer>();
        await redisDbContainer.DisposeAsync();
        var seedNodeContainer = container.Resolve<LighthouseNodeContainer>();
        await seedNodeContainer.DisposeAsync();
        await Task.Delay(750);
    }

    public static ReloadableLogger CreateLogger()
        => new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.Async(write => write.Console())
            .CreateBootstrapLogger();
}