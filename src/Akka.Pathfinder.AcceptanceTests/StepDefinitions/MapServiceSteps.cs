using Akka.Pathfinder.AcceptanceTests.Drivers;
using Akka.Pathfinder.DemoLayout;
using Akka.Pathfinder.Grpc;
using Reqnroll;
using Reqnroll.BoDi;
using Serilog;
using DirectionConfig = Akka.Pathfinder.Grpc.DirectionConfig;
using PointConfig = Akka.Pathfinder.Grpc.PointConfig;

namespace Akka.Pathfinder.AcceptanceTests.StepDefinitions;

[Binding]
public class MapServiceSteps
{
    private readonly ILogger _logger = Log.Logger.ForContext<MapServiceSteps>();
    private readonly ScenarioContext _context;
    private readonly GrpcApplicationFactory _applicationFactory;

    public MapServiceSteps(ScenarioContext context, IObjectContainer container)
    {
        _logger.Information("[TEST][MapServiceSteps][ctor]");
        _context = context;
        _applicationFactory = container.Resolve<GrpcApplicationFactory>();
    }

    [When(@"Create a map with id ""(.*)"" with (.*) points from Map (.*)")]
    public async Task WhenCreateAMapWithIdWithPointsFromMap(string mapId, int expectedPointCount, int mapTemplateId)
    {
        _logger.Information("[TEST][MapServiceSteps][WhenCreateAMapWithIdWithPointsFromMap] MapId: {MapId}, PointCount: {PointCount}, TemplateId: {TemplateId}",
            mapId, expectedPointCount, mapTemplateId);

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(15));
        var mapServiceClient = _applicationFactory.GetMapManagerClient();
        var mapTemplate = MapProvider.MapConfigs.GetValueOrDefault(mapTemplateId)!;

        var createRequest = new CreateRequest
        {
            MapId = mapId
        };

        createRequest.Points.Add(mapTemplate.Configs.Values.SelectMany(x => x).Select(pointConfig =>
        {
            var result = new PointConfig
            {
                Cost = pointConfig.Cost,
                Id = pointConfig.Id
            };
            result.DirectionConfigs.Add(pointConfig.DirectionConfigs.ToDictionary(x => (int)x.Key.To(), x =>
                new DirectionConfig
                {
                    Cost = x.Value.Cost,
                    TargetPointId = x.Value.TargetPointId
                }));
            return result;
        }).ToList());

        var response = await mapServiceClient.CreateAsync(createRequest, cancellationToken: cts.Token);
        _context.Add($"CreateResponse_{mapId}", response);
        _logger.Information("[TEST][MapServiceSteps] Map created with response: Success={Success}, ErrorMessage={ErrorMessage}",
            response.Success, response.ErrorMessage);
    }

    [Then(@"the map creation should be successful for map ""(.*)""")]
    public void ThenTheMapCreationShouldBeSuccessfulForMap(string mapId)
    {
        _logger.Information("[TEST][MapServiceSteps][ThenTheMapCreationShouldBeSuccessfulForMap] MapId: {MapId}", mapId);
        var response = _context.Get<CreateResponse>($"CreateResponse_{mapId}");
        
        Assert.NotNull(response);
        Assert.True(response.Success, $"Map creation failed: {response.ErrorMessage}");
        Assert.Equal(mapId, response.MapId);
        _context.Add($"MapId_{mapId}", mapId);
    }

    [Then(@"the map should contain (.*) points")]
    public void ThenTheMapShouldContainPoints(int expectedPointCount)
    {
        _logger.Information("[TEST][MapServiceSteps][ThenTheMapShouldContainPoints] ExpectedPointCount: {PointCount}", expectedPointCount);
        var lastMapId = _context.Keys
            .Where(k => k.StartsWith("MapId_"))
            .OrderBy(k => k)
            .LastOrDefault();
        
        Assert.NotNull(lastMapId);
        var response = _context.Get<CreateResponse>($"CreateResponse_{_context.Get<string>(lastMapId)}");
        
        Assert.NotNull(response);
        Assert.Equal((uint)expectedPointCount, response.PointCount);
    }

    [When(@"Get the state of map ""(.*)""")]
    public async Task WhenGetTheStateOfMap(string mapId)
    {
        _logger.Information("[TEST][MapServiceSteps][WhenGetTheStateOfMap] MapId: {MapId}", mapId);

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var mapServiceClient = _applicationFactory.GetMapManagerClient();
        var request = new MapRequest { MapId = mapId };

        var response = await mapServiceClient.GetStateAsync(request, cancellationToken: cts.Token);
        _context.Add($"StateResponse_{mapId}", response);
        _logger.Information("[TEST][MapServiceSteps] State retrieved: Success={Success}, IsReady={IsReady}",
            response.Success, response.IsReady);
    }

    [Then(@"the map state should indicate ready status")]
    public void ThenTheMapStateShouldIndicateReadyStatus()
    {
        _logger.Information("[TEST][MapServiceSteps][ThenTheMapStateShouldIndicateReadyStatus]");
        var stateResponse = _context.Get<StateResponse>(_context.Keys.First(k => k.StartsWith("StateResponse_")));
        
        Assert.NotNull(stateResponse);
        Assert.True(stateResponse.Success, $"GetState failed: {stateResponse.ErrorMessage}");
        Assert.True(stateResponse.IsReady, "Map is not ready");
    }

    [When(@"Load map ""(.*)""")]
    public async Task WhenLoadMap(string mapId)
    {
        _logger.Information("[TEST][MapServiceSteps][WhenLoadMap] MapId: {MapId}", mapId);

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(15));
        var mapServiceClient = _applicationFactory.GetMapManagerClient();
        var request = new MapRequest { MapId = mapId };

        var response = await mapServiceClient.LoadAsync(request, cancellationToken: cts.Token);
        _context.Add($"LoadResponse_{mapId}", response);
        _logger.Information("[TEST][MapServiceSteps] Map loaded: Success={Success}", response.Success);
    }

    [Then(@"the map load should be successful")]
    public void ThenTheMapLoadShouldBeSuccessful()
    {
        _logger.Information("[TEST][MapServiceSteps][ThenTheMapLoadShouldBeSuccessful]");
        var loadResponse = _context.Get<Ack>(_context.Keys.First(k => k.StartsWith("LoadResponse_")));
        
        Assert.NotNull(loadResponse);
        Assert.True(loadResponse.Success, "Map load failed");
    }

    [When(@"Delete map ""(.*)""")]
    public async Task WhenDeleteMap(string mapId)
    {
        _logger.Information("[TEST][MapServiceSteps][WhenDeleteMap] MapId: {MapId}", mapId);

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(15));
        var mapServiceClient = _applicationFactory.GetMapManagerClient();
        var request = new MapRequest { MapId = mapId };

        var response = await mapServiceClient.DeleteAsync(request, cancellationToken: cts.Token);
        _context.Add($"DeleteResponse_{mapId}", response);
        _logger.Information("[TEST][MapServiceSteps] Map deleted: Success={Success}", response.Success);
    }

    [Then(@"the map deletion should be successful")]
    public void ThenTheMapDeletionShouldBeSuccessful()
    {
        _logger.Information("[TEST][MapServiceSteps][ThenTheMapDeletionShouldBeSuccessful]");
        var deleteResponse = _context.Get<DeleteResponse>(_context.Keys.First(k => k.StartsWith("DeleteResponse_")));
        
        Assert.NotNull(deleteResponse);
        Assert.True(deleteResponse.Success, $"Map deletion failed: {deleteResponse.ErrorMessage}");
    }

    [Then(@"the map state retrieval should fail")]
    public void ThenTheMapStateRetrievalShouldFail()
    {
        _logger.Information("[TEST][MapServiceSteps][ThenTheMapStateRetrievalShouldFail]");
        var stateResponse = _context.Get<StateResponse>(_context.Keys.First(k => k.StartsWith("StateResponse_")));
        
        Assert.NotNull(stateResponse);
        Assert.False(stateResponse.Success, "GetState should fail for non-existent map");
    }

    [Then(@"the map deletion should fail")]
    public void ThenTheMapDeletionShouldFail()
    {
        _logger.Information("[TEST][MapServiceSteps][ThenTheMapDeletionShouldFail]");
        var deleteResponse = _context.Get<DeleteResponse>(_context.Keys.First(k => k.StartsWith("DeleteResponse_")));
        
        Assert.NotNull(deleteResponse);
        Assert.False(deleteResponse.Success, "Map deletion should fail for non-existent map");
    }

    [Then(@"the map load should fail")]
    public void ThenTheMapLoadShouldFail()
    {
        _logger.Information("[TEST][MapServiceSteps][ThenTheMapLoadShouldFail]");
        var loadResponse = _context.Get<Ack>(_context.Keys.First(k => k.StartsWith("LoadResponse_")));
        
        Assert.NotNull(loadResponse);
        Assert.False(loadResponse.Success, "Map load should fail for non-existent map");
    }
}

