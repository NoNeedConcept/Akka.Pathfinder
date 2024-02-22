namespace Akka.Pathfinder.DemoLayout;

internal class Station(int x, int y, int z, string name, TransportType type)
{
    public int X { get; set; } = x;
    public int Y { get; set; } = y;
    public int Z { get; set; } = z;
    public string Name { get; set; } = name;
    public TransportType Type { get; set; } = type;
    public List<int> ConnectedLines { get; set; } = [];
}