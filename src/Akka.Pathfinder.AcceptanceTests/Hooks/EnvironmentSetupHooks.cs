using Akka.Pathfinder.AcceptanceTests.Containers;
using Akka.Pathfinder.AcceptanceTests.Drivers;
using Akka.Pathfinder.Core;
using Serilog;
using TechTalk.SpecFlow;

namespace Akka.Pathfinder.AcceptanceTests.Hooks;

[Binding]
public class EnvironmentSetupHooks
{
    public static MongoDbContainer MongoDbContainer = null!;
    public static PostgreContainer PostgreContainer = null!;
    public static LighthouseNodeContainer SeedNodeContainer = null!;
    public static PathfinderApplicationFactory PathfinderApplicationFactory = null!;
    public static AkkaDriver AkkaDriver = null!;

    [BeforeFeature]
    public static async Task BeforeFeature()
    {
        Log.Logger = CreateLogger();
        Log.Information("[TEST][EnvironmentSetupHooks][BeforeFeature]");
        MongoDbContainer = new();
        PostgreContainer = new();
        AkkaDriver = new();
        SeedNodeContainer = new();

        var lighthouseTask = SeedNodeContainer.InitializeAsync();
        var mongoTask = MongoDbContainer.InitializeAsync();
        var postgreTask = PostgreContainer.InitializeAsync();

        await lighthouseTask;
        await mongoTask;
        await postgreTask;

        await AkkaDriver.InitializeAsync();

        var mongoDBString = MongoDbContainer.GetConnectionString();
        var postgreSQLString = PostgreContainer.GetConnectionString();

        Log.Debug("[TEST][EnvironmentSetupHooks] - MongoDb: {ConnectionString}", mongoDBString);
        Log.Debug("[TEST][EnvironmentSetupHooks] - Postgre: {ConnectionString}", postgreSQLString);
        AkkaPathfinder.SetEnvironmentVariable("mongodb", mongoDBString);
        AkkaPathfinder.SetEnvironmentVariable("postgre", postgreSQLString);

        PathfinderApplicationFactory = new();
        await PathfinderApplicationFactory.InitializeAsync();

        await Task.Delay(2500);
    }

    [AfterScenario]
    public static void AfterScenario() => Log.Information("[TEST][EnvironmentSetupHooks][AfterScenario]");

    [AfterFeature]
    public static async Task AfterFeature()
    {
        Log.Information("[TEST][EnvironmentSetupHooks][AfterFeature]");

        await PathfinderApplicationFactory.DisposeAsync();
        PathfinderApplicationFactory = null!;
        await MongoDbContainer.DisposeAsync();
        MongoDbContainer = null!;
        await PostgreContainer.DisposeAsync();
        PostgreContainer = null!;
        await SeedNodeContainer.DisposeAsync();
        SeedNodeContainer = null!;
    }

    [AfterTestRun]
    public static async Task AfterTestRun()
    {
        Log.Information("[TEST][EnvironmentSetupHooks][AfterFeature]");

        await (PathfinderApplicationFactory?.DisposeAsync() ?? ValueTask.CompletedTask);
        PathfinderApplicationFactory = null!;
        await (MongoDbContainer?.DisposeAsync() ?? Task.CompletedTask);
        MongoDbContainer = null!;
        await (PostgreContainer?.DisposeAsync() ?? Task.CompletedTask);
        PostgreContainer = null!;
        await (SeedNodeContainer?.DisposeAsync() ?? Task.CompletedTask);
        SeedNodeContainer = null!;
    }

    private static ILogger CreateLogger()
        => new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.Debug()
            .CreateLogger();
}
