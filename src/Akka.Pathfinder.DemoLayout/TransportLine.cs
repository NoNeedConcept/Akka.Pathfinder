namespace Akka.Pathfinder.DemoLayout;

internal class TransportLine(int lineNumber, string name, TransportType trackType)
{
    public int LineNumber { get; set; } = lineNumber;
    public string Name { get; set; } = name;
    public List<Station> Stations { get; set; } = [];
    public TransportType TrackType { get; set; } = trackType;
}