namespace Akka.Pathfinder.DemoLayout;

public record MapConfig(Guid Id, IReadOnlyCollection<Guid> CollectionIds, int Count, int Width = 0, int Height = 0, int Depth = 0)
{ }

public record MapConfigWithPoints(Guid Id, IDictionary<Guid, List<PointConfig>> Configs, int Width = 0, int Height = 0, int Depth = 0) : MapConfig(Id, Configs.Keys.ToList(), Configs.Values.SelectMany(x => x).Count(), Width, Height, Depth);

public record PointConfig(int Id, uint Cost, Dictionary<Directions, DirectionConfig> DirectionConfigs, bool HasChanges = false, string? Name = null);

public record DirectionConfig(int TargetPointId, uint Cost);

[Flags]
public enum Directions : byte
{
    None = 0,
    Top = 1 << 0,
    Bottom = 1 << 1,
    Left = 1 << 2,
    Right = 1 << 3,
    Front = 1 << 4,
    Back = 1 << 5,
    FrontBack = Front | Back,
    LeftRightTopBottom = Left | Right | Top | Bottom,
    All = Top | Bottom | Left | Right | Front | Back
}