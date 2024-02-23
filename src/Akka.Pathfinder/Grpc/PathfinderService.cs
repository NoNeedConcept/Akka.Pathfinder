using Akka.Pathfinder.Core;
using Akka.Pathfinder.Grpc;
using Grpc.Core;

namespace Akka.Pathfinder;

public class PathfinderService : Grpc.Pathfinder.PathfinderBase
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly Serilog.ILogger _logger;

    public PathfinderService(IServiceScopeFactory scopeFactory)
    {
        _logger = Serilog.Log.Logger.ForContext("SourceContext", GetType().Name);
        _serviceScopeFactory = scopeFactory;
    }

    public override async Task FindPath(IAsyncStreamReader<Grpc.FindPathRequest> requestStream, IServerStreamWriter<FindPathResponse> responseStream, ServerCallContext context)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var gateway = scope.ServiceProvider.GetRequiredService<IPathfinderGatewayService>();
            await foreach (var item in requestStream.ReadAllAsync(context.CancellationToken).ConfigureAwait(false))
            {
                var request = item.To();
                var response = await gateway.FindPathAsync(request, context.CancellationToken);
                await responseStream.WriteAsync(response.To(), context.CancellationToken);
            }
        }
        catch (RpcException ex) when (ex.StatusCode != StatusCode.Cancelled)
        {
            _logger.Error(ex, "[Error]][{ErrorMessage}]", ex.Message);
        }
        catch (OperationCanceledException ex)
        {
            _logger.Fatal("[Canceled][{ErrorMessage}]", ex.Message);
        }
    }

    public override async Task<GetPathResponse> GetPath(GetPathRequest request, ServerCallContext context)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var pathReader = scope.ServiceProvider.GetRequiredService<IPathReader>();
            if (!Guid.TryParse(request.PathId, out var pathId) || !Guid.TryParse(request.PathfinderId, out var pathfinderId))
            {
                return new GetPathResponse { Success = false, ErrorMessage = "path id or pathfinder id guid parse failed" };
            }

            var path = pathReader.Get().FirstOrDefault(item => item.Id == pathId && item.PathfinderId == pathfinderId);
            if (path is null)
            {
                return new GetPathResponse { Success = false, ErrorMessage = "no path found in db" };
            }

            await Task.CompletedTask;
            return new GetPathResponse { Success = true, Path = { path.Directions.Select(item => item.To()) } };
        }
        catch (RpcException ex) when (ex.StatusCode != StatusCode.Cancelled)
        {
            _logger.Error(ex, "[Error]][{ErrorMessage}]", ex.Message);
            return new GetPathResponse { Success = false, ErrorMessage = ex.Message };
        }
        catch (OperationCanceledException ex)
        {
            _logger.Fatal("[Canceled][{ErrorMessage}]", ex.Message);
            return new GetPathResponse { Success = false, ErrorMessage = ex.Message };
        }
    }
}
