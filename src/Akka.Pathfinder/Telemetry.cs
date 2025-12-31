using System.Diagnostics;

namespace Akka.Pathfinder;

public static class Telemetry
{
    public static readonly ActivitySource ActivitySource = new("Pathfinder");
}