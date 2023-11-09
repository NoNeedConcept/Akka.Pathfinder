using Akka.Pathfinder.Core.Configs;

namespace Akka.Pathfinder.Core.Persistence;

public record PersistedPointWorkerState(int PointId, Guid CollectionId, uint Cost, IReadOnlyDictionary<Direction, DirectionConfig> DirectionConfigs, PointState State);

