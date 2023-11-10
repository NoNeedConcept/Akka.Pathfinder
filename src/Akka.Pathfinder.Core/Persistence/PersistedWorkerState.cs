using Akka.Pathfinder.Core.Configs;

namespace Akka.Pathfinder.Core.Persistence;

public record PersistedPointWorkerState(int PointId, Guid CollectionId, uint Cost, IReadOnlyDictionary<Direction, DirectionConfig> DirectionConfigs, PointState State);

public record PersistedPathfinderWorkerState(Guid PathfinderId, Direction StartDirection, int SourcePointId, int TargetPointId, TimeSpan Timeout, int FoundPathCounter);