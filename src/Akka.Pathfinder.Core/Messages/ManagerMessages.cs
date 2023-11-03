using Akka.Pathfinder.Core.Configs;

namespace Akka.Pathfinder.Core.Messages;

public record InitializeMap(Guid MapId);

public record InitializePointWorker(PointConfig PointConfig);