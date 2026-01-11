using Akka.Pathfinder.AcceptanceTests.Drivers;
using Akka.Pathfinder.Grpc;
using Reqnroll;
using Reqnroll.BoDi;
using Serilog;

namespace Akka.Pathfinder.AcceptanceTests.StepDefinitions;

[Binding]
public class PathServiceSteps
{
    private readonly ILogger _logger = Log.Logger.ForContext<PathServiceSteps>();
    private readonly ScenarioContext _context;
    private readonly GrpcApplicationFactory _applicationFactory;

    public PathServiceSteps(ScenarioContext context, IObjectContainer container)
    {
        _logger.Information("[TEST][PathServiceSteps][ctor]");
        _context = context;
        _applicationFactory = container.Resolve<GrpcApplicationFactory>();
    }

    [When(@"Get path with id from PathfinderId (.*)")]
    public async Task WhenGetPathWithIdFromPathfinderId(string pathfinderId)
    {
        _logger.Information("[TEST][PathServiceSteps][WhenGetPathWithIdFromPathfinderId] PathfinderId: {PathfinderId}", pathfinderId);

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(15));
        var findPathResponse = _context.Get<FindPathResponse>($"Result_{pathfinderId}");
        Assert.NotNull(findPathResponse);
        Assert.True(findPathResponse.Success, "FindPath must succeed before GetPath");

        var pathServiceClient = _applicationFactory.GetPathfinderClient();
        var request = new GetPathRequest
        {
            PathId = findPathResponse.PathId,
            PathfinderId = pathfinderId
        };

        var response = await pathServiceClient.GetPathAsync(request, cancellationToken: cts.Token);
        _context.Add($"GetPathResponse_{pathfinderId}", response);
        _logger.Information("[TEST][PathServiceSteps] Path retrieved: Success={Success}, PointCount={PointCount}",
            response.Success, response.Path.Count);
    }

    [Then(@"the retrieved path should contain valid points")]
    public void ThenTheRetrievedPathShouldContainValidPoints()
    {
        _logger.Information("[TEST][PathServiceSteps][ThenTheRetrievedPathShouldContainValidPoints]");
        var getPathResponse = _context.Get<GetPathResponse>(_context.Keys.First(k => k.StartsWith("GetPathResponse_")));

        Assert.NotNull(getPathResponse);
        Assert.True(getPathResponse.Success, $"GetPath failed: {getPathResponse.ErrorMessage}");
        Assert.NotEmpty(getPathResponse.Path);
        
        foreach (var point in getPathResponse.Path)
        {
            Assert.NotNull(point);
        }

        _logger.Information("[TEST][PathServiceSteps] Path contains {PointCount} points", getPathResponse.Path.Count);
    }

    [When(@"Delete pathfinder (.*)")]
    public async Task WhenDeletePathfinder(string pathfinderId)
    {
        _logger.Information("[TEST][PathServiceSteps][WhenDeletePathfinder] PathfinderId: {PathfinderId}", pathfinderId);

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(15));
        var pathServiceClient = _applicationFactory.GetPathfinderClient();
        var request = new DeletePathfinderRequest { PathfinderId = pathfinderId };

        var response = await pathServiceClient.DeleteAsync(request, cancellationToken: cts.Token);
        _context.Add($"DeletePathfinderResponse_{pathfinderId}", response);
        _logger.Information("[TEST][PathServiceSteps] Pathfinder deleted: Success={Success}", response.Success);
    }

    [Then(@"the pathfinder deletion should be successful")]
    public void ThenThePathfinderDeletionShouldBeSuccessful()
    {
        _logger.Information("[TEST][PathServiceSteps][ThenThePathfinderDeletionShouldBeSuccessful]");
        var deleteResponse = _context.Get<DeletePathfinderResponse>(_context.Keys.First(k => k.StartsWith("DeletePathfinderResponse_")));

        Assert.NotNull(deleteResponse);
        Assert.True(deleteResponse.Success, $"Pathfinder deletion failed: {deleteResponse.ErrorMessage}");
    }

    [When(@"Get path with non-existent path id ""(.*)""")]
    public async Task WhenGetPathWithNonExistentPathId(string pathId)
    {
        _logger.Information("[TEST][PathServiceSteps][WhenGetPathWithNonExistentPathId] PathId: {PathId}", pathId);

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(15));
        var pathServiceClient = _applicationFactory.GetPathfinderClient();
        var request = new GetPathRequest
        {
            PathId = pathId,
            PathfinderId = "nonexistent"
        };

        var response = await pathServiceClient.GetPathAsync(request, cancellationToken: cts.Token);
        _context.Add("GetPathResponse_NonExistent", response);
        _logger.Information("[TEST][PathServiceSteps] GetPath for non-existent ID: Success={Success}", response.Success);
    }

    [Then(@"the path retrieval should fail")]
    public void ThenThePathRetrievalShouldFail()
    {
        _logger.Information("[TEST][PathServiceSteps][ThenThePathRetrievalShouldFail]");
        var getPathResponse = _context.Get<GetPathResponse>("GetPathResponse_NonExistent");

        Assert.NotNull(getPathResponse);
        Assert.False(getPathResponse.Success, "GetPath should fail for non-existent path ID");
    }

    [Then(@"both paths should be retrievable")]
    public void ThenBothPathsShouldBeRetrievable()
    {
        _logger.Information("[TEST][PathServiceSteps][ThenBothPathsShouldBeRetrievable]");
        var responses = _context.Keys
            .Where(k => k.StartsWith("GetPathResponse_"))
            .Select(k => _context.Get<GetPathResponse>(k))
            .ToList();

        Assert.NotEmpty(responses);
        foreach (var response in responses)
        {
            Assert.NotNull(response);
            Assert.True(response.Success, $"GetPath failed: {response.ErrorMessage}");
            Assert.NotEmpty(response.Path);
        }

        _logger.Information("[TEST][PathServiceSteps] {Count} paths are retrievable", responses.Count);
    }
}

