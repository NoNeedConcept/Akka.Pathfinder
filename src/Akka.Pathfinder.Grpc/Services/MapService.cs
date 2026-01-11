using Akka.Actor;
using Akka.Hosting;
using Akka.Pathfinder.Core;
using Akka.Pathfinder.Core.Configs;
using Akka.Pathfinder.Core.Services;
using Akka.Pathfinder.Grpc.Conversions;
using Akka.Pathfinder.Grpc.Forwarder;
using Grpc.Core;

namespace Akka.Pathfinder.Grpc.Services;

public static class MongoConstantLengthForCollections
{
    public const int Length = 1500000;
}

public class MapService : Grpc.MapService.MapServiceBase
{
    private readonly IActorRef _requestForwarder;
    private readonly IMapConfigWriter _mapConfigWriter;
    private readonly IPointConfigWriter _pointConfigWriter;

    private readonly Serilog.ILogger _logger;

    public MapService(IServiceProvider serviceProvider)
    {
        _logger = Serilog.Log.Logger.ForContext("SourceContext", GetType().Name);
        _requestForwarder = serviceProvider.GetRequiredService<IActorRegistry>().Get<RequestForwarder>();
        _mapConfigWriter = serviceProvider.GetRequiredService<IMapConfigWriter>();
        _pointConfigWriter = serviceProvider.GetRequiredService<IPointConfigWriter>();
    }

    public override async Task<StateResponse> GetState(MapRequest request, ServerCallContext context)
    {
        _logger.Verbose("[{RequestType}][{@Context}]", request.GetType().Name, context);
        try
        {
            var requestItem = request.ToGetMapState();
            var response = await _requestForwarder.AskTraced<Core.Messages.MapStateResponse>(requestItem, context.CancellationToken);
            return response.To();
        }
        catch (RpcException ex) when (ex.StatusCode != StatusCode.Cancelled)
        {
            _logger.Error(ex, "[{RequestType}][{@Context}]", request.GetType(), context);
            return new StateResponse { MapId = Guid.Empty.ToString(), IsReady = false };
        }
        catch (OperationCanceledException)
        {
            return new StateResponse { MapId = Guid.Empty.ToString(), IsReady = false };
        }
    }

    public override async Task<Ack> Load(MapRequest request, ServerCallContext context)
    {
        _logger.Verbose("[{RequestType}][{@Context}]", request.GetType().Name, context);

        try
        {
            var requestItem = request.ToLoadMap();
            var response = await _requestForwarder.AskTraced<Core.Messages.MapLoaded>(requestItem, context.CancellationToken);
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

    public override async Task<DeleteResponse> Delete(MapRequest request, ServerCallContext context)
    {
        _logger.Verbose("[{RequestType}][{@Context}]", request.GetType().Name, context);

        try
        {
            var requestItem = request.ToDeleteMap();
            var response = await _requestForwarder.AskTraced<Core.Messages.MapDeleted>(requestItem, context.CancellationToken);
            return response.To();
        }
        catch (RpcException ex) when (ex.StatusCode != StatusCode.Cancelled)
        {
            _logger.Error(ex, "[{RequestType}][{@Context}]", request.GetType(), context);
            return new DeleteResponse { Success = false, ErrorMessage = ex.Message };
        }
        catch (OperationCanceledException)
        {
            return new DeleteResponse { Success = false, ErrorMessage = "Canceled" };
        }
    }

    public override async Task<CreateResponse> Create(CreateRequest request, ServerCallContext context)
    {
        _logger.Verbose("[{RequestType}][{@Context}]", request.GetType().Name, context);
        try
        {
            var mapId = Guid.Parse(request.MapId);
            List<Guid> collectionIds = [];
            var listOfListOfPoints = request.Points.Select(x => x.To()).Chunk(MongoConstantLengthForCollections.Length)
                .Select(x => x.ToList());
            var ofListOfPoints = listOfListOfPoints.ToArray();
            foreach (var listOfPoints in ofListOfPoints)
            {
                var collectionId = Guid.NewGuid();
                await _pointConfigWriter.AddPointConfigsAsync(collectionId, listOfPoints, context.CancellationToken);
                collectionIds.Add(collectionId);
            }

            var pointCount = ofListOfPoints.Sum(x => x.Count);
            await _mapConfigWriter.WriteAsync(new MapConfig(mapId, collectionIds, pointCount));
            CreateResponse response = new()
                { MapId = request.MapId, Success = true, ErrorMessage = "", PointCount = (uint)pointCount };
            response.CollectionIds.AddRange(collectionIds.Select(x => x.ToString()));
            return response;
        }
        catch (RpcException ex) when (ex.StatusCode != StatusCode.Cancelled)
        {
            _logger.Error(ex, "[{RequestType}][{@Context}]", request.GetType(), context);
            return new CreateResponse { Success = false, ErrorMessage = ex.Message };
        }
        catch (OperationCanceledException)
        {
            return new CreateResponse { Success = false, ErrorMessage = "Canceled" };
        }
    }
}