namespace Akka.Pathfinder.Core.Configs;

public record MapConfig(Guid Id, IReadOnlyCollection<Guid> CollectionIds, int Count);