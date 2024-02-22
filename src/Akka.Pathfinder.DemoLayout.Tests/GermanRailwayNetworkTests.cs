using Akka.Pathfinder.DemoLayout;

namespace Akka.Pathfinder.Layout.Tests;

public class GermanRailwayNetworkTests
{
    [Fact]
    public void Create_ShouldGenerateMapWithCorrectDimensions()
    {
        // Arrange
        var factory = new GermanRailwayNetworkFactory();
        var settings = new GermanRailwayNetworkSettings(Scale: 1);

        // Act
        var mapConfig = factory.Create(settings);

        // Assert
        Assert.Equal(500, mapConfig.Width);
        Assert.Equal(500, mapConfig.Height);
        Assert.Equal(2, mapConfig.Depth);
    }

    [Fact]
    public void Create_ShouldHaveFullConnectivity()
    {
        // Arrange
        var factory = new GermanRailwayNetworkFactory();
        var settings = new GermanRailwayNetworkSettings(Scale: 1);
        var mapConfig = factory.Create(settings);
        var points = mapConfig.Configs.Values.First();

        var trackPoints = points.Where(p => p.Cost < 1000).ToList();
        Assert.NotEmpty(trackPoints);

        // Act
        var startPoint = trackPoints.First();
        var reachable = GetReachablePoints(points, startPoint.Id);

        // Assert
        var unreachableTracks = trackPoints.Where(p => !reachable.Contains(p.Id)).ToList();
        Assert.Empty(unreachableTracks);
    }

    [Theory]
    [InlineData("Hamburg", "Munich")]
    [InlineData("Berlin", "Cologne")]
    [InlineData("Frankfurt am Main", "Leipzig")]
    [InlineData("Stuttgart", "Berlin")]
    [InlineData("Kiel", "Freiburg")]
    public void Create_ShouldConnectMajorCities(string fromCity, string toCity)
    {
        var factory = new GermanRailwayNetworkFactory();
        var mapConfig = factory.Create(new GermanRailwayNetworkSettings(Scale: 1));
        var points = mapConfig.Configs.Values.First();

        var trackPoints = points.Where(p => p.Cost < 1000).ToList();
        var startPoint = trackPoints.First();
        var reachable = GetReachablePoints(points, startPoint.Id);

        Assert.Equal(trackPoints.Count, reachable.Count);
    }

    [Fact]
    public void Create_WithMetro_ShouldIncludeMetroTracks()
    {
        var factory = new GermanRailwayNetworkFactory();
        var settings = new GermanRailwayNetworkSettings(Scale: 1, IncludeMetro: true);
        var mapConfig = factory.Create(settings);
        var points = mapConfig.Configs.Values.First();

        var metroPoints = points.Where(p => p.Cost == TransportCosts.BaseCosts[TransportType.MetroTrack]).ToList();
        Assert.NotEmpty(metroPoints);
    }

    [Fact]
    public void Create_WithMetroAndTram_ShouldIncludeLocalTransit()
    {
        var factory = new GermanRailwayNetworkFactory();
        var settings = new GermanRailwayNetworkSettings(Scale: 1, IncludeMetro: true, IncludeTram: true);
        var mapConfig = factory.Create(settings);
        var points = mapConfig.Configs.Values.First();

        var metroPoints = points.Where(p => p.Cost == TransportCosts.BaseCosts[TransportType.MetroTrack]).ToList();
        var tramPoints = points.Where(p => p.Cost == TransportCosts.BaseCosts[TransportType.TramTrack]).ToList();

        Assert.NotEmpty(metroPoints);
        Assert.NotEmpty(tramPoints);
    }

    [Fact]
    public void Create_ShouldIncludeInfrastructure()
    {
        var factory = new GermanRailwayNetworkFactory();
        var settings = new GermanRailwayNetworkSettings(Scale: 1);
        var mapConfig = factory.Create(settings);
        var points = mapConfig.Configs.Values.First();

        var depotCount = points.Count(p => p.Cost == TransportCosts.BaseCosts[TransportType.Depot]);
        var platformCount = points.Count(p => p.Name != null && p.Name.Contains("Track-"));
        var maintenanceCount = points.Count(p => p.Cost == TransportCosts.BaseCosts[TransportType.MaintenanceArea]);

        Assert.True(depotCount > 0, "Should have depots");
        Assert.True(platformCount > 0, "Should have station platforms");
        Assert.True(maintenanceCount > 0, "Should have maintenance areas");
    }

    [Fact]
    public void Create_ShouldHaveMultipleTracksOnMainCorridors()
    {
        var factory = new GermanRailwayNetworkFactory();
        var mapConfig = factory.Create(new GermanRailwayNetworkSettings(Scale: 1));
        var points = mapConfig.Configs.Values.First();
        var w = mapConfig.Width;
        var h = mapConfig.Height;

        // Hamburg at Y ~ 38
        // Hannover at Y ~ 96
        // Check between them at Y = 60
        var yTest = 60;
        var xAtTest = points.Where(p =>
        {
            var rem = p.Id % (w * h);
            var y = rem / w;
            var z = p.Id / (w * h);
            return z == 1 && y == yTest && p.Cost < 1000;
        }).Select(p => p.Id % w).ToList();
        
        Assert.True(xAtTest.Count >= 2, $"Should have multiple tracks between Hamburg and Hannover at Y={yTest}. Found: {string.Join(", ", xAtTest)}");
    }

    [Fact]
    public void Create_ShouldNotHaveLongJunctionSegments()
    {
        var factory = new GermanRailwayNetworkFactory();
        var settings = new GermanRailwayNetworkSettings(Scale: 1);
        var mapConfig = factory.Create(settings);
        var points = mapConfig.Configs.Values.First();

        var junctionPoints = points.Where(p => p.Cost == TransportCosts.BaseCosts[TransportType.Junction]).ToList();

        foreach (var jPoint in junctionPoints)
        {
            var connectionCount = jPoint.DirectionConfigs.Count;
            
            if (connectionCount <= 2)
            {
                foreach (var conn in jPoint.DirectionConfigs.Values)
                {
                    var neighbor = points.First(p => p.Id == conn.TargetPointId);
                    if (neighbor.Cost == TransportCosts.BaseCosts[TransportType.Junction] &&
                        neighbor.DirectionConfigs.Count <= 2)
                    {
                        Assert.Fail(
                            $"Found Junction segment at {jPoint.Id} and {neighbor.Id}. Junctions should not be used as regular tracks.");
                    }
                }
            }
        }
    }

    [Fact]
    public void Create_ShouldStillHaveJunctionsAtCrossings()
    {
        var factory = new GermanRailwayNetworkFactory();
        var settings = new GermanRailwayNetworkSettings(Scale: 1);
        var mapConfig = factory.Create(settings);
        var points = mapConfig.Configs.Values.First();

        var junctionCount = points.Count(p => p.Cost == TransportCosts.BaseCosts[TransportType.Junction]);
        Assert.True(junctionCount > 0, "Should have junctions at crossings");
    }

    [Fact]
    public void Create_ShouldHaveConsistentCostsAndTypes()
    {
        var factory = new GermanRailwayNetworkFactory();
        var settings = new GermanRailwayNetworkSettings(Scale: 1, DetailLevel.High);
        var mapConfig = factory.Create(settings);
        var points = mapConfig.Configs.Values.First();

        foreach (var point in points)
        {
            if (point.Cost < 1000)
            {
                var type = TransportCosts.GetTypeFromCost(point.Cost);
                Assert.NotEqual(TransportType.Empty, type);
            }

            foreach (var conn in point.DirectionConfigs.Values)
            {
                var targetPoint = points.First(p => p.Id == conn.TargetPointId);
                Assert.Equal(targetPoint.Cost, conn.Cost);
            }
        }
    }

    [Fact]
    public void Create_ShouldKeepDepotsAsDepots()
    {
        var factory = new GermanRailwayNetworkFactory();
        var settings = new GermanRailwayNetworkSettings(Scale: 1);
        var mapConfig = factory.Create(settings);
        var points = mapConfig.Configs.Values.First();

        var depotPositions = GermanRailwayData.Cities
            .Where(c => c.Type == TransportType.Depot)
            .Select(c =>
            {
                const int s = 2; // Forced Scale
                const float cityScale = 1.6f;
                return 1 * (mapConfig.Width * mapConfig.Height) + (int)(c.Y * s * cityScale) * mapConfig.Width + (int)(c.X * s * cityScale);
            }).ToList();

        foreach (var posId in depotPositions)
        {
            var point = points[posId];
            var type = TransportCosts.GetTypeFromCost(point.Cost);
            Assert.Equal(TransportType.Depot, type);
        }
    }

    [Fact]
    public void Create_ShouldPlaceMetroUnderground()
    {
        var factory = new GermanRailwayNetworkFactory();
        var settings = new GermanRailwayNetworkSettings(Scale: 1, IncludeMetro: true);
        var mapConfig = factory.Create(settings);
        var points = mapConfig.Configs.Values.First();

        var metroPoints = points.Where(p => p.Cost == TransportCosts.BaseCosts[TransportType.MetroTrack]).ToList();
        var normalTrackPoints = points.Where(p => p.Cost == TransportCosts.BaseCosts[TransportType.ExpressTrack]
                                                  || p.Cost == TransportCosts.BaseCosts[TransportType.MainTrack])
            .ToList();

        Assert.NotEmpty(metroPoints);
        Assert.NotEmpty(normalTrackPoints);

        foreach (var p in metroPoints)
        {
            var z = p.Id / (mapConfig.Width * mapConfig.Height);
            Assert.Equal(0, z);
        }

        foreach (var p in normalTrackPoints)
        {
            var z = p.Id / (mapConfig.Width * mapConfig.Height);
            Assert.Equal(1, z);
        }
    }

    [Fact]
    public void Create_ShouldIncludeRealisticPlatformCounts()
    {
        var factory = new GermanRailwayNetworkFactory();
        var settings = new GermanRailwayNetworkSettings(
            Scale: 1,
            IncludeMetro: true
        );
        var mapConfig = factory.Create(settings);
        var points = mapConfig.Configs.Values.First();

        var frankfurtPlatforms = points.Count(p => (p.Id / (mapConfig.Width * mapConfig.Height)) == 1
                                                   && p.Name != null && p.Name.Contains("Track-")
                                                   && p.Name.Contains("Frankfurt am Main"));

        var frankfurtUndergroundPlatforms = points.Count(p => (p.Id / (mapConfig.Width * mapConfig.Height)) == 0
                                                              && p.Name != null && p.Name.Contains("Track-")
                                                              && p.Name.Contains("Frankfurt am Main")
                                                              && !p.Name.Contains("Stop-"));

        Assert.Equal(25, frankfurtPlatforms);
        Assert.Equal(4, frankfurtUndergroundPlatforms);

        var muenchenPlatforms = points.Count(p => (p.Id / (mapConfig.Width * mapConfig.Height)) == 1
                                                  && p.Name != null && p.Name.Contains("Track-")
                                                  && p.Name.Contains("Munich"));

        var muenchenUndergroundPlatforms = points.Count(p => (p.Id / (mapConfig.Width * mapConfig.Height)) == 0
                                                             && p.Name != null && p.Name.Contains("Track-")
                                                             && p.Name.Contains("Munich")
                                                             && !p.Name.Contains("Stop-"));

        Assert.Equal(32, muenchenPlatforms);
        Assert.Equal(2, muenchenUndergroundPlatforms);
    }

    [Fact]
    public void Export_ExtremeDetailNetwork_ToHtml()
    {
        var factory = new GermanRailwayNetworkFactory();
        var settings = new GermanRailwayNetworkSettings(
            Detail: DetailLevel.Extreme,
            IncludeMetro: true,
            IncludeTram: true
        );
        var mapConfig = factory.Create(settings);

        const string filePath = "GermanRailwayNetwork_Extreme_Detail.html";
        MapVisualizer.ExportToFile(mapConfig.RemoveIsolatedPoints(), filePath);

        Assert.True(File.Exists(filePath));
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
}
