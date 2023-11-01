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
    public static PostgreContainer PostgreContainer = null!;
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
        PostgreContainer = new PostgreContainer();

        SeedNodeContainer = new();
        var lighthouseTask = SeedNodeContainer.InitializeAsync();
        var mongoTask = MongoDbContainer.InitializeAsync();
        var postgreTask = PostgreContainer.InitializeAsync();

        await lighthouseTask;

        await mongoTask;
        await postgreTask;
        AkkaDriver = new AkkaDriver();
        await AkkaDriver.InitializeAsync();
        var mongoDBString = MongoDbContainer.GetConnectionString();
        var postgreSQLString = PostgreContainer.GetConnectionString();

        Log.Debug("[TEST][EnvironmentSetupHooks] - MongoDb: {ConnectionString}", mongoDBString);
        Log.Debug("[TEST][EnvironmentSetupHooks] - Postgre: {ConnectionString}", postgreSQLString);
        AkkaPathfinder.SetEnvironmentVariable("mongodb", mongoDBString);
        AkkaPathfinder.SetEnvironmentVariable("postgre", postgreSQLString);

        PointConfigDriver = new(MongoDbContainer);

        PathfinderApplicationFactory = new();
        await PathfinderApplicationFactory.InitializeAsync();

        await Task.Delay(2500);
    }

    [AfterScenario]
    public static async Task AfterScenario()
    {
        Log.Information("[TEST][EnvironmentSetupHooks][AfterScenario]");
        await MongoDbContainer.DropDataAsync();
    }

    [AfterTestRun]
    public static async Task AfterTestRun()
    {
        Log.Information("[TEST][EnvironmentSetupHooks][AfterTestRun]");

        // todo: Dispose all containers
        await SeedNodeContainer.DisposeAsync();
        await MongoDbContainer.DisposeAsync();
        await PostgreContainer.DisposeAsync();
        await PathfinderApplicationFactory.DisposeAsync();
    }

    private static Serilog.ILogger CreateLogger() => new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.Debug()
            .CreateLogger();
}
