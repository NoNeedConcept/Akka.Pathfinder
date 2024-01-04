using System.Diagnostics;
using Akka.Pathfinder.AcceptanceTests.Drivers;
using Akka.Pathfinder.AcceptanceTests.Hooks;
using Akka.Pathfinder.Core.Configs;
using Akka.Pathfinder.Core.Messages;
using Akka.Pathfinder.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using TechTalk.SpecFlow;

namespace Akka.Pathfinder.AcceptanceTests.StepDefinitions;

[Binding]
public class PathfinderSteps
{
    private readonly ISpecFlowOutputHelper _specFlowOutputHelper;
    private readonly Serilog.ILogger _logger = Serilog.Log.Logger.ForContext<PathfinderSteps>();
    private readonly ScenarioContext _context;
    private readonly AkkaDriver _akkaDriver;

    private Stopwatch _stopwatch = new();

    public PathfinderSteps(ScenarioContext context, ISpecFlowOutputHelper specFlowOutputHelper)
    {
        _logger.Information("[TEST][PathfinderSteps][ctor]", GetType().Name);
        _context = context;
        _specFlowOutputHelper = specFlowOutputHelper;
        _akkaDriver = EnvironmentSetupHooks.AkkaDriver;
    }

    [Then(@"the path for PathfinderId (.*) should cost (.*)")]
    public void ThenThePathShouldCost(string pathfinderId, int expectedCost)
    {
        var pathFound = _akkaDriver.ReceivePathFound();
        _stopwatch.Stop();
        Assert.NotNull(pathFound);
        Assert.Equal(pathfinderId, pathFound.PathfinderId.ToString());
        Assert.True(pathFound.Success);
        

        var pathReader = _akkaDriver.Host.Services.GetRequiredService<IPathReader>();
        var result = pathReader.Get(pathFound.PathId).Single();
        int actualCost = result.Directions.Select(p => (int)p.Cost).Sum();

        _specFlowOutputHelper.WriteLine($"Best path was found after {_stopwatch.ElapsedMilliseconds} ms");
        Assert.Equal(expectedCost, actualCost);
    }

    [Then(@"the path for PathfinderId (.*) should not be found")]
    public void ThenThePathShouldNotBeFound(string pathfinderId)
    {
        var pathFound = _akkaDriver.ReceivePathFound();

        Assert.NotNull(pathFound);
        Assert.False(pathFound.Success);
        Assert.Equal(pathfinderId, pathFound.PathfinderId.ToString());
    }

    [When(@"You are on Point (.*) and have the direction (.*) want to find a Path to Point (.*) PathfinderId (.*) Seconds (.*)")]
    public void WhenYouAreOnPointWantToFindAPathToPoint(int startPointId, Direction direction, int targetPointId, string pathfinderId, int seconds)
    {
        PathfinderStartRequest request = new(Guid.Parse(pathfinderId), startPointId, targetPointId, direction, TimeSpan.FromSeconds(seconds));
        _stopwatch.Restart();
        _akkaDriver.TellPathfinder(request);
    }
}