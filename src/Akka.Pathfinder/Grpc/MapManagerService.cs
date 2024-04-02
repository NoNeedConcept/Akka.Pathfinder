using Akka.Actor;
using Akka.Hosting;
using Akka.Pathfinder.Core;
using Akka.Pathfinder.Core.Configs;
using Akka.Pathfinder.Core.Messages;
using Akka.Pathfinder.Grpc;
using Akka.Pathfinder.Layout;
using Grpc.Core;
using Microsoft.VisualBasic;

namespace Akka.Pathfinder;

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
            return new Ack() { Success = false };
        }
        catch (OperationCanceledException)
        {
            return new Ack() { Success = false };
        }
    }

    public override async Task<Ack> UpdateMap(MapRequest request, ServerCallContext context)
    {
        _logger.Verbose("[{RequestType}][{@Context}]", request.GetType().Name, context);

        try
        {
            var requestItem = request.ToUpdateMap();
            var response = await _gatewayService.UpdateAsync(requestItem, context.CancellationToken);
            return response.To();
        }
        catch (RpcException ex) when (ex.StatusCode != StatusCode.Cancelled)
        {
            _logger.Error(ex, "[{RequestType}][{@Context}]", request.GetType(), context);
            return new Ack() { Success = false };
        }
        catch (OperationCanceledException)
        {
            return new Ack() { Success = false };
        }
    }

    public override async Task<CreateMapResponse> CreateMap(CreateMapRequest request, ServerCallContext context)
    {
        _logger.Verbose("[{RequestType}][{@Context}]", request.GetType().Name, context);
        try
        {
            var mapId = Guid.Parse(request.MapId);
            List<Guid> collectionIds = [];
            var listOfListOfPoints = request.Points.Select(x => x.To()).Chunk(MongoConstantLengthForCollections.Length).Select(x => x.ToList());
            foreach (var listOfPoints in listOfListOfPoints)
            {
                var collectionId = Guid.NewGuid();
                await _pointConfigWriter.AddPointConfigsAsync(collectionId, listOfPoints, context.CancellationToken);
                collectionIds.Add(collectionId);
            }

            var pointCount = listOfListOfPoints.Sum(x => x.Count);
            await _mapConfigWriter.WriteAsync(new MapConfig(mapId, collectionIds, pointCount));
            CreateMapResponse response = new() { MapId = request.MapId, Success = true, ErrorMessage = "", PointCount = (uint)pointCount };
            response.CollectionIds.AddRange(collectionIds.Select(x => x.ToString()));
            return response;
        }
        catch (RpcException ex) when (ex.StatusCode != StatusCode.Cancelled)
        {
            _logger.Error(ex, "[{RequestType}][{@Context}]", request.GetType(), context);
            return new CreateMapResponse() { Success = false, ErrorMessage = ex.Message };
        }
        catch (OperationCanceledException)
        {
            return new CreateMapResponse() { Success = false, ErrorMessage = "Canceled" };
        }
    }
}

internal interface IMapManagerGatewayService
{
    Task<MapLoaded> LoadAsync(LoadMap request, CancellationToken cancellationToken = default);
    Task<MapUpdated> UpdateAsync(UpdateMap request, CancellationToken cancellationToken = default);
}

internal class MapManagerGatewayService : IMapManagerGatewayService
{
    private readonly IActorRef _mapManagerClient;
    public MapManagerGatewayService(IServiceScopeFactory scopeFactory)
    {
        using var scope = scopeFactory.CreateScope();
        var actorRegistry = scope.ServiceProvider.GetRequiredService<IReadOnlyActorRegistry>();
        _mapManagerClient = actorRegistry.Get<RequestForwarder>();
    }

    public async Task<MapLoaded> LoadAsync(LoadMap request, CancellationToken cancellationToken = default)
        => await _mapManagerClient.Ask<MapLoaded>(request, cancellationToken);

    public async Task<MapUpdated> UpdateAsync(UpdateMap request, CancellationToken cancellationToken = default)
        => await _mapManagerClient.Ask<MapUpdated>(request, cancellationToken);
}