using System.Collections.Immutable;
using Akka.Pathfinder.Core.Configs;

namespace Akka.Pathfinder.Core.Persistence.Data;

public record PathPoint(int PointId, uint Cost, Directions Direction);
public record Path(Guid Id, Guid PathfinderId, Guid RequestId, ImmutableList<PathPoint> Directions);
