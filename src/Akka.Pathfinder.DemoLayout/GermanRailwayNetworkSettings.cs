namespace Akka.Pathfinder.DemoLayout;

public enum DetailLevel
{
    Low,
    High,
    Extreme
}

public record GermanRailwayNetworkSettings(
    int Scale = 2,
    DetailLevel Detail = DetailLevel.Extreme,
    bool IncludeRegionalLines = true,
    bool IncludeMetro = false,
    bool IncludeTram = false
) : IMapSettings;