namespace Akka.Pathfinder.Core.Configs;

public record MapConfig(Guid Id, List<Guid> PointConfigsIds);

public record MapConfigWithPoints(Guid Id, Dictionary<Guid, List<PointConfig>> Configs) : MapConfig(Id, Configs.Keys.ToList());