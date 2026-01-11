using Akka.Pathfinder.AcceptanceTests.Drivers;
using Akka.Pathfinder.Grpc;
using Reqnroll;
using Reqnroll.BoDi;
using Serilog;
using DirectionConfig = Akka.Pathfinder.Grpc.DirectionConfig;
using PointConfig = Akka.Pathfinder.Grpc.PointConfig;

namespace Akka.Pathfinder.AcceptanceTests.StepDefinitions;

public static class Extensions
{
    public static Direction To(this DemoLayout.Directions value)
        => value switch
        {
            DemoLayout.Directions.None => Direction.None,
            DemoLayout.Directions.Top => Direction.Top,
            DemoLayout.Directions.Bottom => Direction.Bottom,
            DemoLayout.Directions.Left => Direction.Left,
            DemoLayout.Directions.Right => Direction.Right,
            DemoLayout.Directions.Front => Direction.Front,
            DemoLayout.Directions.Back => Direction.Back,
            _ => Direction.None
        };
}

[Binding]
public class CommonSteps
{
    private readonly ScenarioContext _context;
    private readonly GrpcApplicationFactory _applicationFactory;
    private readonly ILogger _logger = Log.Logger.ForContext<CommonSteps>();

    public CommonSteps(ScenarioContext context, IObjectContainer container)
    {
        _logger.Information("[TEST][CommonStepDefinitions][ctor]", GetType().Name);
        _context = context;
        _applicationFactory = container.Resolve<GrpcApplicationFactory>();
    }

    [Given(@"Map is (.*)")]
    public async Task GivenMapIs(int mapId)
    {
        _logger.Information("[TEST][CommonStepDefinitions][GivenMapIs] MapId: [{MapId}]", mapId);
        using var source = new CancellationTokenSource(TimeSpan.FromMinutes(15));
        
        try
        {
            var mapServiceClient = _applicationFactory.GetMapManagerClient();
            var mapToLoad = DemoLayout.MapProvider.MapConfigs.GetValueOrDefault(mapId)!;

            var createMap = new CreateRequest
            {
                MapId = mapToLoad.Id.ToString()
            };
            createMap.Points.Add(mapToLoad.Configs.Values.SelectMany(x => x).Select(pointConfig =>
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
            
            _logger.Information("[TEST][CommonStepDefinitions][GivenMapIs] Creating map with {PointCount} points", 
                createMap.Points.Count);
            var response = await mapServiceClient.CreateAsync(createMap, cancellationToken: source.Token);
            Assert.True(response.Success, $"Map creation failed: {response.ErrorMessage}");
            _logger.Information("[TEST][CommonStepDefinitions][GivenMapIs] Map created successfully");
            
            _logger.Information("[TEST][CommonStepDefinitions][GivenMapIs] Loading map");
            var result = await mapServiceClient.LoadAsync(new MapRequest { MapId = mapToLoad.Id.ToString() }, cancellationToken: source.Token);
            Assert.True(result.Success, "Map load failed");
            
            _logger.Information("[TEST][CommonStepDefinitions][GivenMapIs] MapLoaded successfully");
        }
        catch (OperationCanceledException ex)
        {
            _logger.Error("[TEST][CommonStepDefinitions][GivenMapIs] Operation cancelled after 15 minutes: {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error("[TEST][CommonStepDefinitions][GivenMapIs] Error loading map {MapId}: {Exception}", mapId, ex);
            throw;
        }
    }

    [Then("wait {int} min")]
    public async Task ThenWaitIntMin(int delayInMin)
    {
        await Task.Delay(TimeSpan.FromMinutes(delayInMin));
    }
}