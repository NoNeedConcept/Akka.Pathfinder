namespace Akka.Pathfinder.Core.States;

public class MapManagerState
{
    private readonly Serilog.ILogger _logger = Serilog.Log.Logger.ForContext<MapManagerState>();

    public static MapManagerState FromRequest(Guid mapId)
        => new()
        {
            IsMapReady = false,
            MapId = mapId
        };

    public MapManagerState() { }

    public Guid MapId { get; internal set; }
    public bool IsMapReady { get; internal set; } = false;
    public void SetMapIsReady() => IsMapReady = true;
}