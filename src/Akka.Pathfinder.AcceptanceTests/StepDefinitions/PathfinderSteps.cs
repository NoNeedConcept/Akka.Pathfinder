using Akka.Pathfinder.AcceptanceTests.Drivers;
using Akka.Pathfinder.AcceptanceTests.Hooks;
using Akka.Pathfinder.Core.Configs;
using Akka.Pathfinder.Core.Messages;
using Serilog;
using TechTalk.SpecFlow;

namespace Akka.Pathfinder.AcceptanceTests.StepDefinitions;

[Binding]
public class PathfinderSteps
{
    private readonly ScenarioContext _context;
    private readonly AkkaDriver _akkaDriver;

    public PathfinderSteps(ScenarioContext context)
    {
        Log.Information("[TEST][PathfinderSteps][ctor]", GetType().Name);
        _context = context;
        _akkaDriver = EnvironmentSetupHooks.AkkaDriver;
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
        PathfinderStartRequest request = new(Guid.NewGuid(), startPointId, targetPointId, direction, TimeSpan.FromSeconds(15));
        _akkaDriver.TellPathfinder(request);
    }
}