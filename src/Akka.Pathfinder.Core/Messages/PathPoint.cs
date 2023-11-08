using Akka.Pathfinder.Core.Configs;

namespace Akka.Pathfinder.Core.Messages;

public record PathPoint(int PointId, uint Cost, Direction Direction);