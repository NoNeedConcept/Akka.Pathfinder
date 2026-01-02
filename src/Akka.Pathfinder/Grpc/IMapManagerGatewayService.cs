using Akka.Pathfinder.Core.Messages;

namespace Akka.Pathfinder;

internal interface IMapManagerGatewayService
{
    Task<MapStateResponse> GetMapState(GetMapState request, CancellationToken cancellationToken);
    Task<MapLoaded> LoadAsync(LoadMap request, CancellationToken cancellationToken = default);
    Task<MapDeleted> DeleteAsync(DeleteMap request, CancellationToken cancellationToken = default);
}
