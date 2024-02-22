using Akka.Pathfinder.DemoLayout;

namespace Akka.Pathfinder.Layout.Tests;

public class MapProviderTests
{
    [Theory]
    [InlineData(0, 2, 1, 1)]
    [InlineData(1, 3, 3, 1)]
    [InlineData(2, 3, 3, 3)]
    [InlineData(3, 15, 15, 15)]
    [InlineData(6, 500, 500, 2)]
    [InlineData(7, 50, 50, 2)]
    [InlineData(8, 75, 75, 2)]
    public void MapProvider_ShouldHaveCorrectDimensions(int mapId, int expectedWidth, int expectedHeight, int expectedDepth)
    {
        // Arrange & Act
        var mapConfig = MapProvider.MapConfigs[mapId];

        // Assert
        Assert.Equal(expectedWidth, mapConfig.Width);
        Assert.Equal(expectedHeight, mapConfig.Height);
        Assert.Equal(expectedDepth, mapConfig.Depth);
    }
}
