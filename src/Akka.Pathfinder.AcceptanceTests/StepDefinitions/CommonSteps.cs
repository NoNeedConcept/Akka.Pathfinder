using Akka.Pathfinder.AcceptanceTests.Drivers;
using Akka.Pathfinder.AcceptanceTests.Hooks;
using Akka.Pathfinder.Core;
using Akka.Pathfinder.Core.Configs;
using Akka.Pathfinder.Core.Messages;
using Akka.Pathfinder.Core.Services;
using Akka.Pathfinder.Layout;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TechTalk.SpecFlow;

namespace Akka.Pathfinder.AcceptanceTests.StepDefinitions;

[Binding]
public class CommonSteps
{
    private readonly ScenarioContext _context;
    private readonly AkkaDriver _akkaDriver;
    private readonly ILogger _logger = Log.Logger.ForContext<CommonSteps>();

    public CommonSteps(ScenarioContext context)
    {
        _logger.Information("[TEST][CommonStepDefinitions][ctor]", GetType().Name);
        _context = context;
        _akkaDriver = EnvironmentSetupHooks.AkkaDriver;
    }

    [Given(@"Map is (.*)")]
    public async Task GivenMapIs(int mapId)
    {
        _logger.Information("[TEST][CommonStepDefinitions][GivenMapIs] MapId: [{MapId}]", mapId);
        var mapToLoad = new MapProvider().MapConfigs.GetValueOrDefault(mapId)!;
        using var scope = _akkaDriver.Host.Services.CreateScope();
        var mapConfigWriter = scope.ServiceProvider.GetRequiredService<IMapConfigWriter>();
        await mapConfigWriter.WriteAsync(new MapConfig(mapToLoad.Id, mapToLoad.PointConfigsIds, mapToLoad.Count));
        var pointConfigWriter = scope.ServiceProvider.GetRequiredService<IPointConfigWriter>();
        foreach (var (key, value) in mapToLoad.Configs)
        {
            await pointConfigWriter.AddPointConfigsAsync(key, value);
        }

        _akkaDriver.TellMapManager(new LoadMap(mapToLoad.Id));
        _akkaDriver.Expect<MapLoaded>(15000);
        _logger.Information("[TEST][CommonStepDefinitions][GivenMapIs] MapLoaded");
    }
}