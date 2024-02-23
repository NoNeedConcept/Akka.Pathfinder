using Akka.Pathfinder.Core.Configs;

namespace Akka.Pathfinder.Core.Persistence;

public record PersistedInitializedPointState(int PointId, Guid CollectionId);
public record PersistedPointWorkerState(int PointId, Guid CollectionId, uint Cost, Dictionary<Direction, DirectionConfig> DirectionConfigs, PointState State, bool Loaded);

public record PersistedPathfinderWorkerState(Guid PathfinderId, Direction StartDirection, int SourcePointId, int TargetPointId, TimeSpan Timeout, int FoundPathCounter, DateTime StartTime, bool IsFinished = false);