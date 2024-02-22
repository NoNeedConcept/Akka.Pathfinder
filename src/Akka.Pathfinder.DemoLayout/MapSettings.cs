namespace Akka.Pathfinder.DemoLayout;

public record MapSettings(
    uint PointCost,
    uint DefaultDirectionCost,
    MapSize MapSize,
    Dictionary<Directions, uint> DirectionsCosts,
    int Seed = 0) : IMapSettings;

public record IntergalaticDummyMapSettings(
    uint PointCost,
    uint DefaultDirectionCost,
    MapSize MapSize,
    Dictionary<Directions, uint> DirectionsCosts,
    int Seed = 0,
    bool IntergalacticDummyMode = false)
    : MapSettings(PointCost, DefaultDirectionCost, MapSize, DirectionsCosts, Seed);

public record PredefinedMapSettings(
    uint PointCost,
    uint DefaultDirectionCost,
    MapSize MapSize,
    Dictionary<Directions, uint> DirectionsCosts,
    int Seed = 0,
    IDictionary<int, int[,]>? PredefinedMap = null)
    : MapSettings(PointCost, DefaultDirectionCost, MapSize, DirectionsCosts, Seed);