using Akka.Pathfinder.Core.Messages;

namespace Akka.Pathfinder;

internal interface IPathfinderGatewayService
{
    Task<PathfinderResponse> FindPathAsync(PathfinderRequest request, CancellationToken cancellationToken = default);
    Task<PathfinderDeleted> DeleteAsync(DeletePathfinder request, CancellationToken cancellationToken = default);
}
