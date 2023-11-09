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
    private readonly Serilog.ILogger _logger = Serilog.Log.Logger.ForContext<CommonSteps>();

    public CommonSteps(ScenarioContext context)
    {
        _logger.Information("[TEST][CommonStepDefinitions][ctor]", GetType().Name);
        _context = context;
        _akkaDriver = EnvironmentSetupHooks.AkkaDriver;
    }

    [Given(@"Map is (.*)")]
    public void GivenMapIs(int mapId)
    {
        Log.Information("[TEST][CommonStepDefinitions][GivenMapIs] MapId: [{MapId}]", mapId);
        var mapToLoad = new MapProvider().MapConfigs.GetValueOrDefault(mapId)!;
        using var scope = _akkaDriver.Host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMapConfigWriter>().AddOrUpdate(mapToLoad.Id, new MapConfig(mapToLoad.Id, mapToLoad.PointConfigsIds));
        var pointConfigWriter = scope.ServiceProvider.GetRequiredService<IPointConfigWriter>();
        foreach (var (key, value) in mapToLoad.Configs)
        {
            pointConfigWriter.AddPointConfigs(key, value);
        }
        _akkaDriver.TellMapManager(new LoadMap(mapToLoad.Id));
        _ = _akkaDriver.Expect<MapLoaded>(1500);

        Log.Information("[TEST][CommonStepDefinitions][GivenMapIs] MapLoaded", mapId);
    }
}