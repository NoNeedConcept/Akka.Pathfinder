using Akka.Pathfinder.AcceptanceTests.Containers;
using Akka.Pathfinder.AcceptanceTests.Drivers;
using Akka.Pathfinder.Core;
using MongoDB.Driver;
using Serilog;
using TechTalk.SpecFlow;
using MongoDbContainer = Akka.Pathfinder.AcceptanceTests.Containers.MongoDbContainer;

namespace Akka.Pathfinder.AcceptanceTests.Hooks;

[Binding]
public class EnvironmentSetupHooks
{
    public static MongoDbContainer MongoDbContainer = null!;
    public static LighthouseNodeContainer SeedNodeContainer = null!;
    public static PathfinderApplicationFactory PathfinderApplicationFactory = null!;
    public static AkkaDriver AkkaDriver = null!;
    public static PointConfigDriver PointConfigDriver = null!;
    public static Guid DefaultPathfinderId { get; } = Guid.Parse("42069420-6969-6969-6969-420420420420");

    [BeforeTestRun]
    public static async Task BeforeTestRun()
    {
        Log.Information("[TEST][EnvironmentSetupHooks][BeforeTestRun]");
        Serilog.Log.Logger = CreateLogger();
        MongoDbContainer = new MongoDbContainer();

        SeedNodeContainer = new();
        var lighthouseTask = SeedNodeContainer.InitializeAsync();
        var mongoTask = MongoDbContainer.InitializeAsync();

        await lighthouseTask;

        await mongoTask;
        AkkaDriver = new AkkaDriver();
        await AkkaDriver.InitializeAsync();
        var mongoDBString = MongoDbContainer.GetConnectionString();

        Log.Debug("[TEST][EnvironmentSetupHooks] - MongoDb: {ConnectionString}", mongoDBString);
        AkkaPathfinder.SetEnvironmentVariable("mongodb", mongoDBString);

        PointConfigDriver = new(MongoDbContainer);

        PathfinderApplicationFactory = new();
        await PathfinderApplicationFactory.InitializeAsync();

        await Task.Delay(2500);
    }

    [AfterScenario]
    public static async Task AfterScenario()
    {
        Log.Information("[TEST][EnvironmentSetupHooks][AfterScenario]");
        var mongoDbClient = new MongoClient(MongoDbContainer.GetConnectionString());
        await mongoDbClient.DropDatabaseAsync("pathfinder");
    }

    [AfterTestRun]
    public static async Task AfterTestRun()
    {
        Log.Information("[TEST][EnvironmentSetupHooks][AfterTestRun]");

        // todo: Dispose all containers
        await SeedNodeContainer.DisposeAsync();
        await MongoDbContainer.DisposeAsync();
        await PathfinderApplicationFactory.DisposeAsync();
    }

    private static Serilog.ILogger CreateLogger() => new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.Debug()
            .CreateLogger();
}
