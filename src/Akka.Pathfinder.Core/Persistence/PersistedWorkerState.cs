using Akka.Pathfinder.Core.Configs;

namespace Akka.Pathfinder.Core.Persistence;

public record PersistedInitializedPointState(int PointId, Guid CollectionId);

public record PersistedPointWorkerState(
    int PointId,
    Guid CollectionId,
    uint Cost,
    Dictionary<Directions, DirectionConfig> DirectionConfigs,
    PointState State,
    bool Loaded);

public record PersistedPathfinderWorkerState(
    Guid PathfinderId,
    Directions StartDirection,
    int SourcePointId,
    int TargetPointId,
    TimeSpan Timeout,
    int FoundPathCounter,
    DateTime StartTime,
    bool IsFinished = false);

public record PersistedMapManagerState(Guid MapId, bool IsMapReady);