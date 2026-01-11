using Akka.Pathfinder.AcceptanceTests.Drivers;
using Akka.Pathfinder.Grpc;
using Reqnroll;
using Reqnroll.BoDi;
using Serilog;

namespace Akka.Pathfinder.AcceptanceTests.StepDefinitions;

[Binding]
public class PointServiceSteps
{
    private readonly ILogger _logger = Log.Logger.ForContext<PointServiceSteps>();
    private readonly ScenarioContext _context;
    private readonly GrpcApplicationFactory _applicationFactory;
    private bool _lastOperationSuccess = false;

    public PointServiceSteps(ScenarioContext context, IObjectContainer container)
    {
        _logger.Information("[TEST][PointServiceSteps][ctor]");
        _context = context;
        _applicationFactory = container.Resolve<GrpcApplicationFactory>();
    }

    [When(@"Occupy point (.*)")]
    public async Task WhenOccupyPoint(int pointId)
    {
        _logger.Information("[TEST][PointServiceSteps][WhenOccupyPoint] PointId: {PointId}", pointId);
        
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(15));
        var pointServiceClient = _applicationFactory.GetPointServiceClient();
        var request = new PointRequest { PointId = pointId };

        var response = await pointServiceClient.OccupyAsync(request, cancellationToken: cts.Token);
        _lastOperationSuccess = response.Success;
        _context.Add($"PointOperation_{pointId}_Occupy", response);
        _logger.Information("[TEST][PointServiceSteps] Point occupied: Success={Success}", response.Success);
    }

    [When(@"Release point (.*)")]
    public async Task WhenReleasePoint(int pointId)
    {
        _logger.Information("[TEST][PointServiceSteps][WhenReleasePoint] PointId: {PointId}", pointId);
        
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(15));
        var pointServiceClient = _applicationFactory.GetPointServiceClient();
        var request = new PointRequest { PointId = pointId };

        var response = await pointServiceClient.ReleaseAsync(request, cancellationToken: cts.Token);
        _lastOperationSuccess = response.Success;
        _context.Add($"PointOperation_{pointId}_Release", response);
        _logger.Information("[TEST][PointServiceSteps] Point released: Success={Success}", response.Success);
    }

    [When(@"Block point (.*)")]
    public async Task WhenBlockPoint(int pointId)
    {
        _logger.Information("[TEST][PointServiceSteps][WhenBlockPoint] PointId: {PointId}", pointId);
        
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(15));
        var pointServiceClient = _applicationFactory.GetPointServiceClient();
        var request = new PointRequest { PointId = pointId };

        var response = await pointServiceClient.BlockAsync(request, cancellationToken: cts.Token);
        _lastOperationSuccess = response.Success;
        _context.Add($"PointOperation_{pointId}_Block", response);
        _logger.Information("[TEST][PointServiceSteps] Point blocked: Success={Success}", response.Success);
    }

    [When(@"Unblock point (.*)")]
    public async Task WhenUnblockPoint(int pointId)
    {
        _logger.Information("[TEST][PointServiceSteps][WhenUnblockPoint] PointId: {PointId}", pointId);
        
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(15));
        var pointServiceClient = _applicationFactory.GetPointServiceClient();
        var request = new PointRequest { PointId = pointId };

        var response = await pointServiceClient.UnblockAsync(request, cancellationToken: cts.Token);
        _lastOperationSuccess = response.Success;
        _context.Add($"PointOperation_{pointId}_Unblock", response);
        _logger.Information("[TEST][PointServiceSteps] Point unblocked: Success={Success}", response.Success);
    }

    [When(@"Increase cost of point (.*) by (.*)")]
    public async Task WhenIncreaseCostOfPointBy(int pointId, uint costValue)
    {
        _logger.Information("[TEST][PointServiceSteps][WhenIncreaseCostOfPointBy] PointId: {PointId}, CostValue: {CostValue}",
            pointId, costValue);
        
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(15));
        var pointServiceClient = _applicationFactory.GetPointServiceClient();
        var request = new UpdateCostRequest { PointId = pointId, Value = costValue };

        var response = await pointServiceClient.IncreaseCostAsync(request, cancellationToken: cts.Token);
        _lastOperationSuccess = response.Success;
        _context.Add($"PointOperation_{pointId}_IncreaseCost", response);
        _logger.Information("[TEST][PointServiceSteps] Point cost increased: Success={Success}", response.Success);
    }

    [When(@"Decrease cost of point (.*) by (.*)")]
    public async Task WhenDecreaseCostOfPointBy(int pointId, uint costValue)
    {
        _logger.Information("[TEST][PointServiceSteps][WhenDecreaseCostOfPointBy] PointId: {PointId}, CostValue: {CostValue}",
            pointId, costValue);
        
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(15));
        var pointServiceClient = _applicationFactory.GetPointServiceClient();
        var request = new UpdateCostRequest { PointId = pointId, Value = costValue };

        var response = await pointServiceClient.DecreaseCostAsync(request, cancellationToken: cts.Token);
        _lastOperationSuccess = response.Success;
        _context.Add($"PointOperation_{pointId}_DecreaseCost", response);
        _logger.Information("[TEST][PointServiceSteps] Point cost decreased: Success={Success}", response.Success);
    }

    [When(@"Increase direction cost of point (.*) in direction (.*) by (.*)")]
    public async Task WhenIncreaseDirectionCostOfPointInDirectionBy(int pointId, int direction, uint costValue)
    {
        _logger.Information("[TEST][PointServiceSteps][WhenIncreaseDirectionCostOfPointInDirectionBy] PointId: {PointId}, Direction: {Direction}, CostValue: {CostValue}",
            pointId, direction, costValue);
        
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(15));
        var pointServiceClient = _applicationFactory.GetPointServiceClient();
        var grpcDirection = (Direction)direction;
        var request = new UpdateDirectionCostRequest 
        { 
            PointId = pointId, 
            Value = costValue,
            Direction = grpcDirection
        };

        var response = await pointServiceClient.IncreaseDirectionCostAsync(request, cancellationToken: cts.Token);
        _lastOperationSuccess = response.Success;
        _context.Add($"PointOperation_{pointId}_IncreaseDirectionCost_{direction}", response);
        _logger.Information("[TEST][PointServiceSteps] Direction cost increased: Success={Success}", response.Success);
    }

    [When(@"Decrease direction cost of point (.*) in direction (.*) by (.*)")]
    public async Task WhenDecreaseDirectionCostOfPointInDirectionBy(int pointId, int direction, uint costValue)
    {
        _logger.Information("[TEST][PointServiceSteps][WhenDecreaseDirectionCostOfPointInDirectionBy] PointId: {PointId}, Direction: {Direction}, CostValue: {CostValue}",
            pointId, direction, costValue);
        
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(15));
        var pointServiceClient = _applicationFactory.GetPointServiceClient();
        var grpcDirection = (Direction)direction;
        var request = new UpdateDirectionCostRequest 
        { 
            PointId = pointId, 
            Value = costValue,
            Direction = grpcDirection
        };

        var response = await pointServiceClient.DecreaseDirectionCostAsync(request, cancellationToken: cts.Token);
        _lastOperationSuccess = response.Success;
        _context.Add($"PointOperation_{pointId}_DecreaseDirectionCost_{direction}", response);
        _logger.Information("[TEST][PointServiceSteps] Direction cost decreased: Success={Success}", response.Success);
    }

    [When(@"Update direction config for point (.*) with direction (.*)")]
    public async Task WhenUpdateDirectionConfigForPointWithDirection(int pointId, int direction)
    {
        _logger.Information("[TEST][PointServiceSteps][WhenUpdateDirectionConfigForPointWithDirection] PointId: {PointId}, Direction: {Direction}",
            pointId, direction);
        
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(15));
        var pointServiceClient = _applicationFactory.GetPointServiceClient();
        var grpcDirection = (Direction)direction;
        var request = new PointConfig 
        { 
            Id = pointId, 
            Cost = 1
        };
        var dirConfig = new DirectionConfig { Cost = 1, TargetPointId = pointId + 1 };
        request.DirectionConfigs.Add((int)grpcDirection, dirConfig);

        var response = await pointServiceClient.UpdateDirectionAsync(request, cancellationToken: cts.Token);
        _lastOperationSuccess = response.Success;
        _context.Add($"PointOperation_{pointId}_UpdateDirection", response);
        _logger.Information("[TEST][PointServiceSteps] Direction updated: Success={Success}", response.Success);
    }

    [Then(@"the point operation should succeed")]
    public void ThenThePointOperationShouldSucceed()
    {
        _logger.Information("[TEST][PointServiceSteps][ThenThePointOperationShouldSucceed]");
        Assert.True(_lastOperationSuccess, "Last point operation failed");
    }
}

