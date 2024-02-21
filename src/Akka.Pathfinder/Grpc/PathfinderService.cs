using Akka.Actor;
using Akka.Hosting;
using Akka.Pathfinder.Core;
using Akka.Pathfinder.Core.Messages;
using Akka.Pathfinder.Grpc;
using Grpc.Core;

namespace Akka.Pathfinder;

public class PathfinderService : Grpc.Pathfinder.PathfinderBase
{
    private readonly IPathfinderGatewayService _gateway;
    private readonly IPathReader _pathReader;
    private readonly Serilog.ILogger _logger = Serilog.Log.Logger.ForContext<PathfinderService>();

    public PathfinderService(IServiceScopeFactory scopeFactory)
    {
        using var scope = scopeFactory.CreateScope();
        _gateway = scope.ServiceProvider.GetRequiredService<IPathfinderGatewayService>();
        _pathReader = scope.ServiceProvider.GetRequiredService<IPathReader>();
    }

    public override async Task FindPath(IAsyncStreamReader<Grpc.FindPathRequest> requestStream, IServerStreamWriter<FindPathResponse> responseStream, ServerCallContext context)
    {
        try
        {
            await foreach (var item in requestStream.ReadAllAsync(context.CancellationToken).ConfigureAwait(false))
            {
                var request = item.To();
                var response = await _gateway.FindPathAsync(request, context.CancellationToken);
                await responseStream.WriteAsync(response.To(), context.CancellationToken);
            }
        }
        catch (RpcException ex) when (ex.StatusCode != StatusCode.Cancelled)
        {
            _logger.Error(ex, "[{@Context}]", context);
        }
        catch (OperationCanceledException)
        { }
    }

    public override Task<GetPathResponse> GetPath(GetPathRequest request, ServerCallContext context)
    {
        return base.GetPath(request, context);
    }
}

internal interface IPathfinderGatewayService
{
    Task<PathfinderResponse> FindPathAsync(PathfinderRequest request, CancellationToken cancellationToken = default);
}

internal class PathfinderGatewayService : IPathfinderGatewayService
{
    private readonly IActorRef _pathfinderClient;
    public PathfinderGatewayService(IServiceScopeFactory scopeFactory)
    {
        using var scope = scopeFactory.CreateScope();
        var actorRegistry = scope.ServiceProvider.GetRequiredService<IReadOnlyActorRegistry>();
        _pathfinderClient = actorRegistry.Get<RequestForwarder>();
    }

    public async Task<PathfinderResponse> FindPathAsync(PathfinderRequest request, CancellationToken cancellationToken = default)
        => await _pathfinderClient.Ask<PathfinderResponse>(request, cancellationToken);
}