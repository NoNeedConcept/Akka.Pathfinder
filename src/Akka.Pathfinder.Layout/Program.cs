using System.Diagnostics;
using Akka.Pathfinder.Core.Configs;
using Akka.Pathfinder.Layout;


int x = 50;
var mapSize = new MapSize(x, x, x);
var sw = Stopwatch.StartNew();
var mapConfig = MapFactory.Create(new MapSettings(42, 20, mapSize, new Dictionary<Direction, uint>()
{
    { Direction.Top, 100 },
    { Direction.Bottom, 100 }
}));
sw.Stop();

Console.WriteLine($"TOTAL POSSIBLE POINTS: {x * x * x}");
Console.WriteLine($"Map Size RAW: {x}x{x}x{x}");
Console.WriteLine($"TOTAL POINTS CREATED: {mapConfig.Points.Count}");
Console.WriteLine(sw.Elapsed);
Console.ReadLine();