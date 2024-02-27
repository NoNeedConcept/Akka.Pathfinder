using Akka.Pathfinder.AcceptanceTests.Containers;
using Akka.Pathfinder.AcceptanceTests.Drivers;
using Akka.Pathfinder.Core;
using BoDi;
using Serilog;
using TechTalk.SpecFlow;

namespace Akka.Pathfinder.AcceptanceTests.Hooks;

[Binding]
public class EnvironmentSetupHooks
{
    [BeforeFeature]
    public static async Task BeforeFeature(ObjectContainer container)
    {
        Log.Logger = CreateLogger();
        Log.Information("[TEST][EnvironmentSetupHooks][BeforeFeature]");
        var mongoDbContainer = new MongoDbContainer();
        var postgreContainer = new PostgreContainer();
        var seedNodeContainer = new LighthouseNodeContainer();

        var lighthouseTask = seedNodeContainer.InitializeAsync();
        var mongoTask = mongoDbContainer.InitializeAsync();
        var postgreTask = postgreContainer.InitializeAsync();

        await lighthouseTask;
        await mongoTask;
        await postgreTask;

        var mongoDBString = mongoDbContainer.GetConnectionString();
        var postgreSQLString = postgreContainer.GetConnectionString();

        Log.Information("[TEST][EnvironmentSetupHooks] - MongoDb: {ConnectionString}", mongoDBString);
        Log.Information("[TEST][EnvironmentSetupHooks] - Postgre: {ConnectionString}", postgreSQLString);
        AkkaPathfinder.SetEnvironmentVariable("mongodb", mongoDBString);
        AkkaPathfinder.SetEnvironmentVariable("postgre", postgreSQLString);

        //var akkaDriver = new AkkaDriver();
        //await akkaDriver.InitializeAsync();

        var pathfinderApplicationFactory = new PathfinderApplicationFactory();
        await pathfinderApplicationFactory.InitializeAsync();

        container.RegisterInstanceAs(mongoDbContainer);
        container.RegisterInstanceAs(postgreContainer);
        //container.RegisterInstanceAs(akkaDriver);
        container.RegisterInstanceAs(seedNodeContainer);
        container.RegisterInstanceAs(pathfinderApplicationFactory);

        await Task.Delay(750);
    }

    [AfterScenario]
    public static void AfterScenario() => Log.Information("[TEST][EnvironmentSetupHooks][AfterScenario]");

    [AfterFeature]
    public static async Task AfterFeature(ObjectContainer container)
    {
        Log.Information("[TEST][EnvironmentSetupHooks][AfterFeature]");

        var pathfinderApplicationFactory = container.Resolve<PathfinderApplicationFactory>();
        await pathfinderApplicationFactory.DisposeAsync();
        //var akkaDriver = container.Resolve<AkkaDriver>();
        //await akkaDriver.DisposeAsync();
        var mongoDbContainer = container.Resolve<MongoDbContainer>();
        await mongoDbContainer.DisposeAsync();
        var postgreContainer = container.Resolve<PostgreContainer>();
        await postgreContainer.DisposeAsync();
        var seedNodeContainer = container.Resolve<LighthouseNodeContainer>();
        await seedNodeContainer.DisposeAsync();
        await Task.Delay(2500);
    }

    private static ILogger CreateLogger()
        => new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Async(write => write.Console())
            .WriteTo.Async(write => write.Debug())
            .CreateLogger();
}
