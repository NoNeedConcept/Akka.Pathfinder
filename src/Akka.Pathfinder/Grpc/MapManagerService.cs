using Akka.Actor;
using Akka.Hosting;
using Akka.Pathfinder.Core.Messages;
using Akka.Pathfinder.Grpc;
using Grpc.Core;

namespace Akka.Pathfinder;

public class MapManagerService : MapManager.MapManagerBase
{
    private readonly IMapManagerGatewayService _gatewayService;

    private readonly Serilog.ILogger _logger = Serilog.Log.Logger.ForContext<MapManagerService>();
    public MapManagerService(IServiceScopeFactory scopeFactory)
    {
        using var scope = scopeFactory.CreateScope();
        _gatewayService = scope.ServiceProvider.GetRequiredService<IMapManagerGatewayService>();
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