using Akka.Pathfinder.AcceptanceTests.Drivers;
using Akka.Pathfinder.Core.Configs;
using BoDi;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using TechTalk.SpecFlow;

namespace Akka.Pathfinder.AcceptanceTests.StepDefinitions;

[Binding]
public class PathfinderSteps
{
    private readonly Serilog.ILogger _logger = Serilog.Log.Logger.ForContext<PathfinderSteps>();
    private readonly ScenarioContext _context;
    private readonly DatabaseDriver _databaseDriver;
    private readonly PathfinderApplicationFactory _applicationFactory;

    public PathfinderSteps(ScenarioContext context, ObjectContainer container)
    {
        _logger.Information("[TEST][PathfinderSteps][ctor]", GetType().Name);
        _context = context;
        _databaseDriver = container.Resolve<DatabaseDriver>();
        _applicationFactory = container.Resolve<PathfinderApplicationFactory>();
    }

    [Then(@"the path for PathfinderId (.*) should cost (.*)")]
    public void ThenThePathShouldCost(string pathfinderId, int expectedCost)
    {
        var pathFound = _context.Get<Grpc.FindPathResponse>($"Result_{pathfinderId}");
        Assert.NotNull(pathFound);
        Assert.Equal(pathfinderId, pathFound.PathfinderId.ToString());
        Assert.True(pathFound.Success);

        var pathReader = _databaseDriver.CreatePathWriter();
        Assert.True(Guid.TryParse(pathFound.PathId, out var pathId));
        var result = pathReader.Get(pathId).Single();
        int actualCost = result.Directions.Select(p => (int)p.Cost).Sum();
        Assert.Equal(expectedCost, actualCost);
    }

    [Then(@"the path for PathfinderId (.*) should not be found")]
    public void ThenThePathShouldNotBeFound(string pathfinderId)
    {
        var pathFound = _context.Get<Grpc.FindPathResponse>($"Result_{pathfinderId}");

        Assert.NotNull(pathFound);
        Assert.False(pathFound.Success);
        Assert.Equal(pathfinderId, pathFound.PathfinderId.ToString());
    }

    [When(@"You are on Point (.*) and have the direction (.*) want to find a Path to Point (.*) PathfinderId (.*) Seconds (.*)")]
    public async Task WhenYouAreOnPointWantToFindAPathToPoint(int startPointId, Direction direction, int targetPointId, string pathfinderId, int seconds)
    {
        var source = new CancellationTokenSource(TimeSpan.FromMinutes(7));
        var request = new Grpc.FindPathRequest()
        {
            PathfinderId = pathfinderId,
            Direction = direction.To(),
            Duration = Duration.FromTimeSpan(TimeSpan.FromSeconds(seconds)),
            SourcePointId = startPointId,
            TargetPointId = targetPointId
        };

        var pathfinderClient = _applicationFactory.GetPathfinderClient();
        var result = pathfinderClient.FindPath(cancellationToken: source.Token);
        try
        {
            await result.RequestStream.WriteAsync(request);
            await foreach (var response in result.ResponseStream.ReadAllAsync())
            {
                _context.Add($"Result_{pathfinderId}", response!);
                await result.RequestStream.CompleteAsync();
                source.Cancel();
            }
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
        { }
    }
}