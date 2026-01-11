using Akka.Pathfinder.AcceptanceTests.Drivers;
using Akka.Pathfinder.DemoLayout;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Reqnroll;
using Reqnroll.BoDi;

namespace Akka.Pathfinder.AcceptanceTests.StepDefinitions;

[Binding]
public class PathfinderSteps
{
    private readonly Serilog.ILogger _logger = Serilog.Log.Logger.ForContext<PathfinderSteps>();
    private readonly ScenarioContext _context;
    private readonly DatabaseDriver _databaseDriver;
    private readonly GrpcApplicationFactory _applicationFactory;

    public PathfinderSteps(ScenarioContext context, ObjectContainer container)
    {
        _logger.Information("[TEST][PathfinderSteps][ctor]{PropertyValue0}", GetType().Name);
        _context = context;
        _databaseDriver = container.Resolve<DatabaseDriver>();
        _applicationFactory = container.Resolve<GrpcApplicationFactory>();
    }

    [Then(@"the path for PathfinderId (.*) should cost (.*)")]
    public void ThenThePathForPathfinderIdShouldCost(string pathfinderId, int expectedCost)
    {
        var contextKey = $"Result_{pathfinderId}";
        Assert.True(_context.ContainsKey(contextKey),
            $"Path result for PathfinderId '{pathfinderId}' not found in context");

        var pathFound = _context.Get<Grpc.FindPathResponse>(contextKey);
        Assert.NotNull(pathFound);
        Assert.Equal(pathfinderId.ToLower(), pathFound.PathfinderId.ToLower(), ignoreCase: true);
        Assert.True(pathFound.Success,
            $"FindPath failed for PathfinderId {pathfinderId}: {pathFound.ErrorMessage}");

        var pathReader = _databaseDriver.CreatePathReader();
        Assert.True(Guid.TryParse(pathFound.PathId, out var pathId),
            $"Invalid path ID format: {pathFound.PathId}");

        var results = pathReader.Get(pathId).ToList();
        Assert.NotEmpty(results);
        Assert.Single(results);

        var result = results[0];
        Assert.NotNull(result);
        if (expectedCost == 0) return;

        var actualCost = result.Directions.Select(p => (int)p.Cost).Sum();
        Assert.Equal(expectedCost, actualCost);
        _logger.Information("[TEST][PathfinderSteps] Path cost validated: PathfinderId={PathfinderId}, Cost={Cost}",
            pathfinderId, actualCost);
    }

    [Then(@"the path for PathfinderId (.*) should cost more than (.*)")]
    public void ThenThePathForPathfinderIdShouldCostMoreThan(string pathfinderId, int minCost)
    {
        var contextKey = $"Result_{pathfinderId}";
        Assert.True(_context.ContainsKey(contextKey),
            $"Path result for PathfinderId '{pathfinderId}' not found");

        var pathFound = _context.Get<Grpc.FindPathResponse>(contextKey);
        Assert.NotNull(pathFound);
        Assert.True(pathFound.Success,
            $"FindPath failed: {pathFound.ErrorMessage}");

        var pathReader = _databaseDriver.CreatePathReader();
        Assert.True(Guid.TryParse(pathFound.PathId, out var pathId));

        var results = pathReader.Get(pathId).ToList();
        Assert.NotEmpty(results);
        Assert.Single(results);

        var result = results[0];
        Assert.NotNull(result);

        var actualCost = result.Directions.Select(p => (int)p.Cost).Sum();
        Assert.True(actualCost > minCost,
            $"Expected cost > {minCost}, but was {actualCost}");
        _logger.Information(
            "[TEST][PathfinderSteps] Path cost validated: PathfinderId={PathfinderId}, MinCost={MinCost}, ActualCost={ActualCost}",
            pathfinderId, minCost, actualCost);
    }

    [Then(@"the path for PathfinderId (.*) should not be found")]
    public void ThenThePathForPathfinderIdShouldNotBeFound(string pathfinderId)
    {
        var pathFound = _context.Get<Grpc.FindPathResponse>($"Result_{pathfinderId}");

        Assert.NotNull(pathFound);
        Assert.False(pathFound.Success);
        Assert.Equal(pathfinderId, pathFound.PathfinderId);
    }

    [When(
        @"You are on Point (.*) and have the direction (.*) want to find a Path to Point (.*) PathfinderId (.*) Seconds (.*)")]
    public async Task WhenYouAreOnPointAndHaveTheDirectionWantToFindAPathToPointPathfinderIdSeconds(int startPointId,
        Directions direction, int targetPointId, string pathfinderId, int seconds)
    {
        _logger.Information(
            "[TEST][PathfinderSteps] Starting pathfinding: Start={StartPoint}, Target={TargetPoint}, Duration={Seconds}s",
            startPointId, targetPointId, seconds);

        using var source = new CancellationTokenSource(TimeSpan.FromMinutes(15));
        var request = new Grpc.FindPathRequest
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
            await result.RequestStream.WriteAsync(request, source.Token);
            await result.RequestStream.CompleteAsync();

            await foreach (var response in result.ResponseStream.ReadAllAsync(cancellationToken: source.Token))
            {
                _context.Add($"Result_{pathfinderId}", response!);
                _logger.Information(
                    "[TEST][PathfinderSteps] Path found: PathfinderId={PathfinderId}, Cost={Cost}, Success={Success}",
                    response.PathfinderId, response.PathCost, response.Success);
            }
        }
        catch (RpcException ex)
        {
            _logger.Error("[TEST][PathfinderSteps] gRPC Error: {StatusCode} - {Message}",
                ex.StatusCode, ex.Message);
            Assert.Fail($"gRPC call failed with status {ex.StatusCode}: {ex.Message}");
        }
        catch (OperationCanceledException ex)
        {
            _logger.Error("[TEST][PathfinderSteps] Operation cancelled: {Message}", ex.Message);
            Assert.Fail($"Operation cancelled after 15 minutes: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.Error("[TEST][PathfinderSteps] Unexpected error in pathfinding: {Exception}", ex);
            throw;
        }
    }
}