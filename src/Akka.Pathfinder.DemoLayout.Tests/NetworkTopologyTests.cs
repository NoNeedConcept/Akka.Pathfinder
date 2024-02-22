using Akka.Pathfinder.DemoLayout;

namespace Akka.Pathfinder.Layout.Tests;

public class NetworkTopologyTests
{
    [Fact]
    public void GeneratedNetwork_ShouldHaveValidTopology()
    {
        // Arrange
        var factory = new GermanRailwayNetworkFactory();
        var settings = new GermanRailwayNetworkSettings(Scale: 1, IncludeMetro: true);
        var mapConfig = factory.Create(settings);
        var points = mapConfig.Configs.Values.First().ToDictionary(p => p.Id);
        
        var width = mapConfig.Width;
        var height = mapConfig.Height;

        foreach (var point in points.Values)
        {
            // Skip empty points
            if (point is { Cost: >= 1000, DirectionConfigs.Count: 0 })
                continue;

            var (x1, y1, z1) = IdToCoordinates(point.Id, width, height);

            foreach (var (direction, value) in point.DirectionConfigs)
            {
                var targetId = value.TargetPointId;
                
                Assert.True(points.ContainsKey(targetId), $"Point {point.Id} connects to existing point {targetId}");
                var targetPoint = points[targetId];
                var (x2, y2, z2) = IdToCoordinates(targetId, width, height);

                // 1. Check: Manhattan distance must be 1 (direct neighbors only)
                var distance = Math.Abs(x1 - x2) + Math.Abs(y1 - y2) + Math.Abs(z1 - z2);
                Assert.Equal(1, distance);

                // 2. Check: Symmetry (back connection must exist)
                var oppositeDir = GetOppositeDirection(direction);
                Assert.True(targetPoint.DirectionConfigs.ContainsKey(oppositeDir), 
                    $"Missing back connection from {targetId} to {point.Id} (expected: {oppositeDir})");
                Assert.Equal(point.Id, targetPoint.DirectionConfigs[oppositeDir].TargetPointId);

                // 3. Check: Direction consistency
                VerifyDirection(x1, y1, z1, x2, y2, z2, direction);
            }

            // 4. Check: Junction logic
            // If a point has more than 2 connections, it must be a Junction, Station or a special type
            if (point.DirectionConfigs.Count > 2)
            {
                var type = TransportCosts.GetTypeFromCost(point.Cost);
                var isJunctionOrStation = type is TransportType.Junction 
                                              or TransportType.Station 
                                              or TransportType.Terminal 
                                              or TransportType.Depot;

                var isZone = type is TransportType.MaintenanceArea;
                
                Assert.True(isJunctionOrStation || isZone, 
                    $"Point {point.Id} at ({x1},{y1},{z1}) has {point.DirectionConfigs.Count} connections, but is of type {type}");
            }
            
            // 5. Check: Minimum connections for junctions
            if (TransportCosts.GetTypeFromCost(point.Cost) == TransportType.Junction)
            {
                Assert.True(point.DirectionConfigs.Count >= 3,
                    $"Point {point.Id} at ({x1},{y1},{z1}) is marked as a junction, but only has {point.DirectionConfigs.Count} connections");
            }
        }
    }

    private static (int x, int y, int z) IdToCoordinates(int id, int width, int height)
    {
        var z = id / (width * height);
        var rem = id % (width * height);
        var y = rem / width;
        var x = rem % width;
        return (x, y, z);
    }

    private static Directions GetOppositeDirection(Directions dir)
    {
        return dir switch
        {
            Directions.Left => Directions.Right,
            Directions.Right => Directions.Left,
            Directions.Top => Directions.Bottom,
            Directions.Bottom => Directions.Top,
            Directions.Front => Directions.Back,
            Directions.Back => Directions.Front,
            _ => Directions.None
        };
    }

    private static void VerifyDirection(int x1, int y1, int z1, int x2, int y2, int z2, Directions dir)
    {
        switch (dir)
        {
            case Directions.Right: Assert.True(x2 > x1 && y1 == y2 && z1 == z2); break;
            case Directions.Left: Assert.True(x2 < x1 && y1 == y2 && z1 == z2); break;
            case Directions.Bottom: Assert.True(y2 > y1 && x1 == x2 && z1 == z2); break;
            case Directions.Top: Assert.True(y2 < y1 && x1 == x2 && z1 == z2); break;
            case Directions.Back: Assert.True(z2 > z1 && x1 == x2 && y1 == y2); break;
            case Directions.Front: Assert.True(z2 < z1 && x1 == x2 && y1 == y2); break;
        }
    }
}
