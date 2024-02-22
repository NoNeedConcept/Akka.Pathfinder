using Akka.Pathfinder.DemoLayout;

namespace Akka.Pathfinder.Layout.Tests;

public class MapVisualizerTests
{
    [Fact]
    public void ExportGermanRailway_ShouldCreateHtmlFile()
    {
        // Arrange
        var factory = MapFactoryProvider.Instance.CreateFactory(FactoryType.GermanyRailway);
        var settings = new GermanRailwayNetworkSettings(
            Scale: 1,
            Detail: DetailLevel.Low,
            IncludeRegionalLines: true
        );
        
        var mapConfig = factory.Create(settings);
        const string filePath = "german_railway_map_debug.html";

        // Act
        MapVisualizer.ExportToFile(mapConfig, filePath);

        // Assert
        Assert.True(File.Exists(filePath));
        var content = File.ReadAllText(filePath);
        Assert.Contains("<!DOCTYPE html>", content);
    }
}
