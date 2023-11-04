using Akka.Pathfinder.Core.Messages;

namespace Akka.Pathfinder.Core.Persistence.Data;

public record Path(Guid Id, Guid PathfinderId, IReadOnlyList<PathPoint> Directions);
