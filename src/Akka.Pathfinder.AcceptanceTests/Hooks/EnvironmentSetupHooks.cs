using Akka.Pathfinder.AcceptanceTests.Containers;
using Akka.Pathfinder.AcceptanceTests.Drivers;
using Akka.Pathfinder.Core;
using MongoDB.Bson.IO;
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
        BsonShit.Register();
        Log.Logger = CreateLogger();
        Log.Information("[TEST][EnvironmentSetupHooks][BeforeTestRun]");
    }

    [BeforeFeature]
    public static async Task BeforeFeature(ObjectContainer container, FeatureContext featureContext)
    {
        Log.Logger = CreateLogger();
        Log.Information("[TEST][EnvironmentSetupHooks][BeforeFeature] Feature: {FeatureTitle}",
            featureContext.FeatureInfo.Title);

        try
        {
            var isRedisForPersistence = featureContext.FeatureInfo.Tags.Contains("RedisActive");
            var mongoDbContainer = new MongoDbContainer();
            var redisDbContainer = new RedisContainer();
            var seedNodeContainer = new LighthouseNodeContainer();

            Log.Information("[TEST][EnvironmentSetupHooks] Initializing containers...");
            var lighthouseTask = seedNodeContainer.InitializeAsync();
            var mongoTask = mongoDbContainer.InitializeAsync();
            var redisTask = redisDbContainer.InitializeAsync();

            await Task.WhenAll(lighthouseTask, mongoTask, redisTask);
            Log.Information("[TEST][EnvironmentSetupHooks] All containers initialized");

            var redisDbString = redisDbContainer.GetConnectionString();
            var mongoDbString = mongoDbContainer.GetConnectionString();
            var seedNodeString = seedNodeContainer.GetSeedNodeString();

            if (isRedisForPersistence)
            {
                Log.Information("[TEST][EnvironmentSetupHooks] Redis enabled - {ConnectionString}", redisDbString);
                Environment.SetEnvironmentVariable("ConnectionStrings__redis", redisDbString);
            }

            Log.Information("[TEST][EnvironmentSetupHooks] MongoDB - {ConnectionString}", mongoDbString);
            Environment.SetEnvironmentVariable("ConnectionStrings__mongodb", mongoDbString);

            Environment.SetEnvironmentVariable("TESTING", "1");
            Environment.SetEnvironmentVariable("akka__cluster__seed-nodes__0", seedNodeString);
            Environment.SetEnvironmentVariable("akka__remote__dot-netty__tcp__public-hostname", "host.docker.internal");

            Log.Information("[TEST][EnvironmentSetupHooks] Initializing application factories...");
            var pathfinderApplicationFactory = new PathfinderApplicationFactory();
            var grpcApplicationFactory = new GrpcApplicationFactory();
            var pathfinderTask = pathfinderApplicationFactory.InitializeAsync();
            var grpcTask = grpcApplicationFactory.InitializeAsync();

            await Task.WhenAll(grpcTask, pathfinderTask);
            Log.Information("[TEST][EnvironmentSetupHooks] All application factories initialized");

            container.RegisterInstanceAs(mongoDbContainer);
            container.RegisterInstanceAs(redisDbContainer);
            container.RegisterInstanceAs(seedNodeContainer);
            container.RegisterInstanceAs(pathfinderApplicationFactory);
            container.RegisterInstanceAs(grpcApplicationFactory);

            Log.Information("[TEST][EnvironmentSetupHooks][BeforeFeature] Setup completed successfully");
        }
        catch (Exception ex)
        {
            Log.Error("[TEST][EnvironmentSetupHooks][BeforeFeature] Critical error during setup: {Exception}", ex);
            throw;
        }
    }

    [AfterScenario]
    public static void AfterScenario() => Log.Information("[TEST][EnvironmentSetupHooks][AfterScenario]");

    [AfterFeature]
    public static async Task AfterFeature(ObjectContainer container)
    {
        try
        {
            Log.Information("[TEST][EnvironmentSetupHooks][AfterFeature] Starting cleanup");

            try
            {
                var pathfinderApplicationFactory = container.Resolve<PathfinderApplicationFactory>();
                await pathfinderApplicationFactory.DisposeAsync();
                Log.Information("[TEST] PathfinderApplicationFactory disposed");
            }
            catch (Exception ex)
            {
                Log.Error("[TEST] Error disposing PathfinderApplicationFactory: {Exception}", ex);
            }

            try
            {
                var grpcApplicationFactory = container.Resolve<GrpcApplicationFactory>();
                await grpcApplicationFactory.DisposeAsync();
                Log.Information("[TEST] GrpcApplicationFactory disposed");
            }
            catch (Exception ex)
            {
                Log.Error("[TEST] Error disposing GrpcApplicationFactory: {Exception}", ex);
            }

            try
            {
                var mongoDbContainer = container.Resolve<MongoDbContainer>();
                await mongoDbContainer.DisposeAsync();
                Log.Information("[TEST] MongoDbContainer disposed");
            }
            catch (Exception ex)
            {
                Log.Error("[TEST] Error disposing MongoDbContainer: {Exception}", ex);
            }

            try
            {
                var redisDbContainer = container.Resolve<RedisContainer>();
                await redisDbContainer.DisposeAsync();
                if (Environment.GetEnvironmentVariable("ConnectionStrings__redis") is not null)
                {
                    Environment.SetEnvironmentVariable("ConnectionStrings__redis", null);
                }

                Log.Information("[TEST] RedisDbContainer disposed");
            }
            catch (Exception ex)
            {
                Log.Error("[TEST] Error disposing RedisDbContainer: {Exception}", ex);
            }

            try
            {
                var seedNodeContainer = container.Resolve<LighthouseNodeContainer>();
                await seedNodeContainer.DisposeAsync();
                Log.Information("[TEST] LighthouseNodeContainer disposed");
            }
            catch (Exception ex)
            {
                Log.Error("[TEST] Error disposing LighthouseNodeContainer: {Exception}", ex);
            }

            await Task.Delay(1500);
            Log.Information("[TEST][EnvironmentSetupHooks][AfterFeature] Cleanup completed");
        }
        catch (Exception ex)
        {
            Log.Error("[TEST][EnvironmentSetupHooks][AfterFeature] Critical error during cleanup: {Exception}", ex);
            throw;
        }
    }

    public static ReloadableLogger CreateLogger()
        => new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.Async(write => write.Console())
            .CreateBootstrapLogger();
}