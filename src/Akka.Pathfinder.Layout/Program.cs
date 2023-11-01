using System.Diagnostics;
using Akka.Dispatch.SysMsg;
using Akka.Pathfinder.Layout;


int x = 200;
var mapSize = new MapSize(x, x, x);
var sw =Stopwatch.StartNew();
var mapConfig = MapFactory.Create(5153143,mapSize);
sw.Stop();

Console.WriteLine($"TOTAL POSSIBLE POINTS: {x*x*x}" );
Console.WriteLine($"Map Size RAW: {x}x{x}x{x}" );
Console.WriteLine($"TOTAL POINTS CREATED: {mapConfig.Points.Count}");
Console.WriteLine(sw.Elapsed);
Console.ReadLine();