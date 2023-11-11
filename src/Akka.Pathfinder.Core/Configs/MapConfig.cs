﻿namespace Akka.Pathfinder.Core.Configs;

public record MapConfig(Guid Id, IReadOnlyCollection<Guid> PointConfigsIds, int Count);

public record MapConfigWithPoints(Guid Id, IDictionary<Guid, List<PointConfig>> Configs) : MapConfig(Id, Configs.Keys.ToList(), Configs.Values.SelectMany(x => x).Count());