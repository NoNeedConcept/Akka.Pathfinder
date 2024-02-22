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
        using var source = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var mapManagerClient = _applicationFactory.GetMapManagerClient();
        var mapToLoad = DemoLayout.MapProvider.MapConfigs.GetValueOrDefault(mapId)!;

        var createMap = new CreateMapRequest
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
        var response = await mapManagerClient.CreateMapAsync(createMap, cancellationToken: source.Token);
        Assert.True(response.Success);
        var result = await mapManagerClient.LoadAsync(new MapRequest { MapId = mapToLoad.Id.ToString() }, cancellationToken: source.Token);

        Assert.True(result.Success);
        _logger.Information("[TEST][CommonStepDefinitions][GivenMapIs] MapLoaded");
    }

    [Then("wait {int} min")]
    public async Task ThenWaitMin(int delayInMin)
    {
        await Task.Delay(TimeSpan.FromMinutes(delayInMin));
    }
}