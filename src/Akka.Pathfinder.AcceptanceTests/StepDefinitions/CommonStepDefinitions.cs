using Akka.Pathfinder.AcceptanceTests.Drivers;
using Akka.Pathfinder.AcceptanceTests.Hooks;
using Akka.Pathfinder.AcceptanceTests.InitialData;
using Akka.Pathfinder.Core.Configs;
using Akka.Pathfinder.Core.Messages;
using Serilog;
using TechTalk.SpecFlow;

namespace Akka.Pathfinder.AcceptanceTests.StepDefinitions;

[Binding]
public class CommonStepDefinitions
{
    private readonly PointConfigDriver _pointConfigDriver;
    private readonly AkkaDriver _akkaDriver;

    public CommonStepDefinitions()
    {
        Log.Information("[TEST][CommonStepDefinitions][ctor]", GetType().Name);

        _pointConfigDriver = EnvironmentSetupHooks.PointConfigDriver;
        _akkaDriver = EnvironmentSetupHooks.AkkaDriver;
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

    [Then(@"the path should cost (.*)")]
    public void ThenThePathShouldCost(int expectedCost)
    {
        var pathFound = _akkaDriver.ReceivePathFound();

        Assert.NotNull(pathFound.Path);

        int actualCost = pathFound.Path.Directions.Select(p => (int)p.Cost).Sum();

        Assert.Equal(expectedCost, actualCost);
    }

    [Then(@"the path should not be found")]
    public void ThenThePathShouldNotBeFound()
    {
        var pathFound = _akkaDriver.ReceivePathFound();

        Assert.Null(pathFound.Path);
    }

    [When(@"You are on Point (.*) and have the direction (.*) want to find a Path to Point (.*)")]
    public void WhenYouAreOnPointWantToFindAPathToPoint(int startPointId, Direction direction, int targetPointId)
    {

        PathfinderStartRequest request = new(Guid.NewGuid(), startPointId, targetPointId, direction);

        _akkaDriver.RequestPathfinder(request);
    }
}