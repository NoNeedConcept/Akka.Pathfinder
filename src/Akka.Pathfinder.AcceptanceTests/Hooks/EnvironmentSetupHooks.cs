using Akka.Pathfinder.AcceptanceTests.Containers;
using Akka.Pathfinder.AcceptanceTests.Drivers;
using Reqnroll;
using Reqnroll.BoDi;
using Serilog;
using Serilog.Extensions.Hosting;

namespace Akka.Pathfinder.AcceptanceTests.Hooks;

[Binding]
public static class EnvironmentSetupHooks
{
    [BeforeFeature]
    public static async Task BeforeFeature(ObjectContainer container)
    {
        Log.Logger = CreateLogger();
        Log.Information("[TEST][EnvironmentSetupHooks][BeforeFeature]");
        var mongoDbContainer = new MongoDbContainer();
        var seedNodeContainer = new LighthouseNodeContainer();

        var lighthouseTask = seedNodeContainer.InitializeAsync();
        var mongoTask = mongoDbContainer.InitializeAsync();

        await lighthouseTask;
        await mongoTask;

        var mongoDBString = mongoDbContainer.GetConnectionString();
        Log.Information("[TEST][EnvironmentSetupHooks] - MongoDb: {ConnectionString}", mongoDBString);
        Environment.SetEnvironmentVariable("ConnectionStrings__mongodb", mongoDBString);
        var pathfinderApplicationFactory = new PathfinderApplicationFactory();
        await pathfinderApplicationFactory.InitializeAsync();

        container.RegisterInstanceAs(mongoDbContainer);
        container.RegisterInstanceAs(seedNodeContainer);
        container.RegisterInstanceAs(pathfinderApplicationFactory);
    }

    [AfterScenario]
    public static void AfterScenario() => Log.Information("[TEST][EnvironmentSetupHooks][AfterScenario]");

    [AfterFeature]
    public static async Task AfterFeature(ObjectContainer container)
    {
        Log.Information("[TEST][EnvironmentSetupHooks][AfterFeature]");
        var pathfinderApplicationFactory = container.Resolve<PathfinderApplicationFactory>();
        await pathfinderApplicationFactory.DisposeAsync();
        var mongoDbContainer = container.Resolve<MongoDbContainer>();
        await mongoDbContainer.DisposeAsync();
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
