using Akka.Pathfinder.Core.Configs;

namespace Akka.Pathfinder.Core.Persistence.Data;

public record PathPoint(int PointId, uint Cost, Direction Direction);
public record Path(Guid Id, Guid PathfinderId, IReadOnlyList<PathPoint> Directions);
