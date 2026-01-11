using System.Diagnostics;
using Akka.Actor;
using Akka.Hosting;
using Akka.Pathfinder.Core;
using Akka.Pathfinder.Core.Messages;
using Akka.Pathfinder.Core.Services;
using Akka.Pathfinder.Grpc.Conversions;
using Akka.Pathfinder.Grpc.Forwarder;
using Akka.Streams;
using Akka.Streams.Dsl;
using Grpc.Core;
using MongoDB.Driver.Linq;

namespace Akka.Pathfinder.Grpc.Services;

public class PathService : Grpc.PathService.PathServiceBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IActorRegistry _actorRegistry;
    private readonly IPathReader _pathReader;
    private readonly Serilog.ILogger _logger;

    public PathService(IServiceProvider serviceProvider)
    {
        _logger = Serilog.Log.Logger.ForContext("SourceContext", GetType().Name);
        _serviceProvider = serviceProvider;
        _actorRegistry = serviceProvider.GetRequiredService<IActorRegistry>();
        _pathReader = serviceProvider.GetRequiredService<IPathReader>();
    }

    public override async Task FindPath(IAsyncStreamReader<FindPathRequest> requestStream,
        IServerStreamWriter<FindPathResponse> responseStream, ServerCallContext context)
    {
        try
        {
            var materializer = _serviceProvider.GetRequiredService<ActorSystem>().Materializer();

            var parentTraceId = Activity.Current?.TraceId.ToHexString();
            var parentSpanId = Activity.Current?.SpanId.ToHexString();
            var requestForwarder = await _actorRegistry.GetAsync<RequestForwarder>();
            var sink = Sink
                .ForEachAsync<FindPathResponse>(1,
                    async response => await responseStream.WriteAsync(response, context.CancellationToken));
            await Source
                .From(() => requestStream.ReadAllAsync(context.CancellationToken))
                .Select(request =>
                {
                    _logger.Information("[FindPath] Received gRPC request");
                    return request.To().WithTracing(parentTraceId, parentSpanId);
                })
                .Ask<PathfinderResponse>(requestForwarder, TimeSpan.FromMinutes(10), 4)
                .Select(response =>
                {
                    _logger.Information("[FindPath] Received Actor response: {@Response}", response);
                    return response.To();
                })
                .RunWith(sink, materializer);
        }
        catch (RpcException ex) when (ex.StatusCode != StatusCode.Cancelled)
        {
            _logger.Error(ex, "[Error]][{ErrorMessage}]", ex.Message);
        }
        catch (OperationCanceledException ex)
        {
            _logger.Error(ex, "[Canceled][{ErrorMessage}]", ex.Message);
        }
    }

    public override async Task<GetPathResponse> GetPath(GetPathRequest request, ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.PathId, out var pathId) ||
                !Guid.TryParse(request.PathfinderId, out var pathfinderId))
            {
                return new GetPathResponse
                    { Success = false, ErrorMessage = "path id or pathfinder id guid parse failed" };
            }

            var path = await _pathReader.Get()
                .FirstOrDefaultAsync(item => item.Id == pathId && item.PathfinderId == pathfinderId);
            if (path is null)
            {
                return new GetPathResponse { Success = false, ErrorMessage = "no path found in db" };
            }

            return new GetPathResponse { Success = true, Path = { path.Directions.Select(item => item.To()) } };
        }
        catch (RpcException ex) when (ex.StatusCode != StatusCode.Cancelled)
        {
            _logger.Error(ex, "[Error]][{ErrorMessage}]", ex.Message);
            return new GetPathResponse { Success = false, ErrorMessage = ex.Message };
        }
        catch (OperationCanceledException ex)
        {
            _logger.Error(ex, "[Canceled][{ErrorMessage}]", ex.Message);
            return new GetPathResponse { Success = false, ErrorMessage = ex.Message };
        }
    }

    public override async Task<DeletePathfinderResponse> Delete(DeletePathfinderRequest request,
        ServerCallContext context)
    {
        try
        {
            var response =
                await _actorRegistry.Get<RequestForwarder>()
                    .AskTraced<PathfinderDeleted>(request.To(), context.CancellationToken);
            return response.To();
        }
        catch (RpcException ex) when (ex.StatusCode != StatusCode.Cancelled)
        {
            _logger.Error(ex, "[Error]][{ErrorMessage}]", ex.Message);
            return new DeletePathfinderResponse { Success = false, ErrorMessage = ex.Message };
        }
        catch (OperationCanceledException ex)
        {
            _logger.Error(ex, "[Canceled][{ErrorMessage}]", ex.Message);
            return new DeletePathfinderResponse { Success = false, ErrorMessage = ex.Message };
        }
    }
}