using Akka.Pathfinder.AcceptanceTests.Drivers;
using Akka.Pathfinder.Core.Configs;
using Akka.Pathfinder.Grpc;
using Akka.Pathfinder.Layout;
using BoDi;
using Serilog;
using TechTalk.SpecFlow;

namespace Akka.Pathfinder.AcceptanceTests.StepDefinitions;

[Binding]
public class CommonSteps
{
    private readonly ScenarioContext _context;
    private readonly DatabaseDriver _databaseDriver;
    private readonly PathfinderApplicationFactory _applicationFactory;
    private readonly ILogger _logger = Log.Logger.ForContext<CommonSteps>();

    public CommonSteps(ScenarioContext context, IObjectContainer container)
    {
        _logger.Information("[TEST][CommonStepDefinitions][ctor]", GetType().Name);
        _context = context;
        _databaseDriver = container.Resolve<DatabaseDriver>();
        _applicationFactory = container.Resolve<PathfinderApplicationFactory>();
    }

    [Given(@"Map is (.*)")]
    public async Task GivenMapIs(int mapId)
    {
        _logger.Information("[TEST][CommonStepDefinitions][GivenMapIs] MapId: [{MapId}]", mapId);
        var mapToLoad = new MapProvider().MapConfigs.GetValueOrDefault(mapId)!;

        var mapConfigWriter = _databaseDriver.CreateMapConfigWriter();
        await mapConfigWriter.WriteAsync(new MapConfig(mapToLoad.Id, mapToLoad.CollectionIds, mapToLoad.Count));
        var pointConfigWriter = _databaseDriver.CreatePointConfigWriter();
        foreach (var (key, value) in mapToLoad.Configs)
        {
            await pointConfigWriter.AddPointConfigsAsync(key, value);
        }

        var mapManagerClient = _applicationFactory.GetMapManagerClient();
        var result = mapManagerClient.Load(new MapRequest() { MapId = mapToLoad.Id.ToString() });
        
        Assert.True(result.Success);
        _logger.Information("[TEST][CommonStepDefinitions][GivenMapIs] MapLoaded");
    }
}