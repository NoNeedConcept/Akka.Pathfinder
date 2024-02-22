using Akka.Pathfinder.DemoLayout;

namespace Akka.Pathfinder.Layout.Tests;

public class GermanRailwayDetailLevelTests
{
    [Fact]
    public void Create_LowDetail_ShouldHaveSmallerDimensions()
    {
        var factory = new GermanRailwayNetworkFactory();
        var settings = new GermanRailwayNetworkSettings(Scale: 1, Detail: DetailLevel.Low);
        
        var mapConfig = factory.Create(settings);
        
        Assert.Equal(50, mapConfig.Width);
        Assert.Equal(50, mapConfig.Height);
    }

    [Fact]
    public void Create_LowDetail_HamburgToHanover_ShouldBeConnected()
    {
        var mapConfig = MapProvider.MapConfigs[7];
        var points = mapConfig.Configs.Values.First().ToDictionary(p => p.Id);
        
        // IDs: Hamburg (2720), Hanover (3118)
        Assert.True(points.ContainsKey(2720), "Hamburg ID 2720 should exist on Map 7");
        Assert.True(points.ContainsKey(3118), "Hanover ID 3118 should exist on Map 7");
        Assert.Equal("Hamburg", points[2720].Name);
        Assert.Equal("Hanover", points[3118].Name);

        var reachable = GetReachablePoints(points.Values.ToList(), 2720);
        Assert.Contains(3118, reachable);
    }

    [Fact]
    public void Create_HighDetail_ShouldHaveMediumDimensions()
    {
        var factory = new GermanRailwayNetworkFactory();
        var settings = new GermanRailwayNetworkSettings(Scale: 1, Detail: DetailLevel.High);
        
        var mapConfig = factory.Create(settings);
        
        Assert.Equal(75, mapConfig.Width);
        Assert.Equal(75, mapConfig.Height);
    }

    [Fact]
    public void Create_HighDetail_HamburgToHanover_ShouldBeConnected()
    {
        var mapConfig = MapProvider.MapConfigs[8];
        var points = mapConfig.Configs.Values.First().ToDictionary(p => p.Id);
        
        // IDs: Hamburg (6180), Hanover (7002)
        Assert.True(points.ContainsKey(6180), "Hamburg ID 6180 should exist on Map 8");
        Assert.True(points.ContainsKey(7002), "Hanover ID 7002 should exist on Map 8");
        Assert.Equal("Hamburg", points[6180].Name);
        Assert.Equal("Hanover", points[7002].Name);

        var reachable = GetReachablePoints(points.Values.ToList(), 6180);
        Assert.Contains(7002, reachable);
    }

    private static HashSet<int> GetReachablePoints(List<PointConfig> points, int startId)
    {
        var pointsById = points.ToDictionary(p => p.Id);
        var visited = new HashSet<int>();
        var queue = new Queue<int>();

        if (pointsById.ContainsKey(startId))
        {
            queue.Enqueue(startId);
            visited.Add(startId);
        }

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            var currentPoint = pointsById[currentId];

            foreach (var directionConfig in currentPoint.DirectionConfigs.Values)
            {
                if (visited.Add(directionConfig.TargetPointId))
                {
                    queue.Enqueue(directionConfig.TargetPointId);
                }
            }
        }

        return visited;
    }

    [Fact]
    public void Create_HighDetail_ShouldHavePlatformsButNoDepots()
    {
        var factory = new GermanRailwayNetworkFactory();
        var settings = new GermanRailwayNetworkSettings(Scale: 1, Detail: DetailLevel.High);
        
        var mapConfig = factory.Create(settings);
        var points = mapConfig.Configs.Values.First();
        
        var platformCount = points.Count(p => p.Name != null && p.Name.Contains("Track-"));
        var depotCount = points.Count(p => p.Cost == TransportCosts.BaseCosts[TransportType.Depot]);
        
        Assert.True(platformCount > 0);
        Assert.Equal(0, depotCount);
    }
}
