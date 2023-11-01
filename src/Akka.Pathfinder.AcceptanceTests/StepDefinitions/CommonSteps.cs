using Akka.Pathfinder.AcceptanceTests.Drivers;
using Akka.Pathfinder.AcceptanceTests.Hooks;
using Akka.Pathfinder.AcceptanceTests.InitialData;
using Serilog;
using TechTalk.SpecFlow;

namespace Akka.Pathfinder.AcceptanceTests.StepDefinitions;

[Binding]
public class CommonSteps
{
    private readonly ScenarioContext _context;
    private readonly PointConfigDriver _pointConfigDriver;

    public CommonSteps(ScenarioContext context)
    {
        Log.Information("[TEST][CommonStepDefinitions][ctor]", GetType().Name);
        _context = context;
        _pointConfigDriver = EnvironmentSetupHooks.PointConfigDriver;
    }

    [Given(@"Map is (.*)")]
    public async Task GivenMapIs(int mapId)
    {
        var mapToLoad = MapProvider.MapConfigs[mapId];

        foreach (var pointConfig in mapToLoad.Points)
        {
            await _pointConfigDriver.AddPointConfig(pointConfig);
        }
    }
}