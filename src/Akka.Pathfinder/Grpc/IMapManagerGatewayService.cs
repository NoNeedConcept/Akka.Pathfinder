using Akka.Pathfinder.Core.Messages;

namespace Akka.Pathfinder;

internal interface IMapManagerGatewayService
{
    Task<MapLoaded> LoadAsync(LoadMap request, CancellationToken cancellationToken = default);
    Task<MapUpdated> UpdateAsync(UpdateMap request, CancellationToken cancellationToken = default);
}
