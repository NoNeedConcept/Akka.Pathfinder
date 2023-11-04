namespace Akka.Pathfinder.Core.Configs;

public record MapConfig(Guid Id, Guid PointConfigsId);

public record MapConfigWithPoints(Guid Id, Guid PointConfigsId, List<PointConfig> Configs) : MapConfig(Id, PointConfigsId);