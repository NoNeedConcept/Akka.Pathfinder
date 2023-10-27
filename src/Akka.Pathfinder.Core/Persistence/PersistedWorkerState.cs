using Akka.Pathfinder.Core.Configs;

namespace Akka.Pathfinder.Core.Persistence;

public record PersistedPointWorkerState(int PointId, uint Cost, IReadOnlyDictionary<Direction, DirectionConfig> DirectionConfigs, PointState State);

