namespace Akka.Pathfinder.DemoLayout;

public static class MapConfigExtensions
{
    public static MapConfigWithPoints RemoveIsolatedPoints(this MapConfigWithPoints mapConfig)
    {
        var newConfigs = new Dictionary<Guid, List<PointConfig>>();

        foreach (var kvp in mapConfig.Configs)
        {
            // 1. Filter out all points with cost >= 1000 (Empty)
            var filteredPoints = kvp.Value
                .Where(p => p.Cost < 1000)
                .ToList();
            
            var validIds = filteredPoints.Select(p => p.Id).ToHashSet();

            // 2. Clean up connections to removed points
            var optimizedPoints = filteredPoints.Select(p => {
                var validDirections = p.DirectionConfigs
                    .Where(d => validIds.Contains(d.Value.TargetPointId))
                    .ToDictionary(x => x.Key, x => x.Value);
                
                if (validDirections.Count == p.DirectionConfigs.Count)
                    return p;
                    
                return p with { DirectionConfigs = validDirections };
            }).ToList();

            newConfigs[kvp.Key] = optimizedPoints;
        }

        return mapConfig with { Configs = newConfigs };
    }
}
