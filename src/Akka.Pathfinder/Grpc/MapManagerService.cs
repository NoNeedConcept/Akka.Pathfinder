using Akka.Pathfinder.Core;
using Akka.Pathfinder.Core.Configs;
using Akka.Pathfinder.Grpc;
using Grpc.Core;

namespace Akka.Pathfinder;

public static class MongoConstantLengthForCollections
{
    public const int Length = 1500000;
}

public class MapManagerService : MapManager.MapManagerBase
{
    private readonly IMapManagerGatewayService _gatewayService;
    private readonly IMapConfigWriter _mapConfigWriter;
    private readonly IPointConfigWriter _pointConfigWriter;

    private readonly Serilog.ILogger _logger;

    public MapManagerService(IServiceScopeFactory scopeFactory)
    {
        _logger = Serilog.Log.Logger.ForContext("SourceContext", GetType().Name);
        using var scope = scopeFactory.CreateScope();
        _gatewayService = scope.ServiceProvider.GetRequiredService<IMapManagerGatewayService>();
        _mapConfigWriter = scope.ServiceProvider.GetRequiredService<IMapConfigWriter>();
        _pointConfigWriter = scope.ServiceProvider.GetRequiredService<IPointConfigWriter>();
    }

    public override async Task<MapStateResponse> GetMapState(MapRequest request, ServerCallContext context)
    {
        _logger.Verbose("[{RequestType}][{@Context}]", request.GetType().Name, context);
        try
        {
            var requestItem = request.ToGetMapState();
            var response = await _gatewayService.GetMapState(requestItem, context.CancellationToken);
            return response.To();
        }
        catch (RpcException ex) when (ex.StatusCode != StatusCode.Cancelled)
        {
            _logger.Error(ex, "[{RequestType}][{@Context}]", request.GetType(), context);
            return new MapStateResponse { MapId = Guid.Empty.ToString(), IsReady = false };
        }
        catch (OperationCanceledException)
        {
            return new MapStateResponse { MapId = Guid.Empty.ToString(), IsReady = false };
        }
    }

    public override async Task<Ack> Load(MapRequest request, ServerCallContext context)
    {
        _logger.Verbose("[{RequestType}][{@Context}]", request.GetType().Name, context);

        try
        {
            var requestItem = request.ToLoadMap();
            var response = await _gatewayService.LoadAsync(requestItem, context.CancellationToken);
            return response.To();
        }
        catch (RpcException ex) when (ex.StatusCode != StatusCode.Cancelled)
        {
            _logger.Error(ex, "[{RequestType}][{@Context}]", request.GetType(), context);
            return new Ack { Success = false };
        }
        catch (OperationCanceledException)
        {
            return new Ack { Success = false };
        }
    }

    public override async Task<DeleteMapResponse> Delete(MapRequest request, ServerCallContext context)
    {
        _logger.Verbose("[{RequestType}][{@Context}]", request.GetType().Name, context);

        try
        {
            var requestItem = request.ToDeleteMap();
            var response = await _gatewayService.DeleteAsync(requestItem, context.CancellationToken);
            return response.To();
        }
        catch (RpcException ex) when (ex.StatusCode != StatusCode.Cancelled)
        {
            _logger.Error(ex, "[{RequestType}][{@Context}]", request.GetType(), context);
            return new DeleteMapResponse { Success = false, ErrorMessage = ex.Message };
        }
        catch (OperationCanceledException)
        {
            return new DeleteMapResponse { Success = false, ErrorMessage = "Canceled" };
        }
    }

    public override async Task<CreateMapResponse> CreateMap(CreateMapRequest request, ServerCallContext context)
    {
        _logger.Verbose("[{RequestType}][{@Context}]", request.GetType().Name, context);
        try
        {
            var mapId = Guid.Parse(request.MapId);
            List<Guid> collectionIds = [];
            var listOfListOfPoints = request.Points.Select(x => x.To()).Chunk(MongoConstantLengthForCollections.Length)
                .Select(x => x.ToList());
            foreach (var listOfPoints in listOfListOfPoints)
            {
                var collectionId = Guid.NewGuid();
                await _pointConfigWriter.AddPointConfigsAsync(collectionId, listOfPoints, context.CancellationToken);
                collectionIds.Add(collectionId);
            }

            var pointCount = listOfListOfPoints.Sum(x => x.Count);
            await _mapConfigWriter.WriteAsync(new MapConfig(mapId, collectionIds, pointCount));
            CreateMapResponse response = new()
                { MapId = request.MapId, Success = true, ErrorMessage = "", PointCount = (uint)pointCount };
            response.CollectionIds.AddRange(collectionIds.Select(x => x.ToString()));
            return response;
        }
        catch (RpcException ex) when (ex.StatusCode != StatusCode.Cancelled)
        {
            _logger.Error(ex, "[{RequestType}][{@Context}]", request.GetType(), context);
            return new CreateMapResponse { Success = false, ErrorMessage = ex.Message };
        }
        catch (OperationCanceledException)
        {
            return new CreateMapResponse { Success = false, ErrorMessage = "Canceled" };
        }
    }
}