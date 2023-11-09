using Akka.Pathfinder.Core.Messages;

namespace Akka.Pathfinder.Core.States;

public class MapManagerState
{
    private readonly Serilog.ILogger _logger = Serilog.Log.Logger.ForContext<MapManagerState>();

    public static MapManagerState FromRequest(LoadMap request)
        => new()
        {
            IsMapReady = false,
            MapId = request.MapId
        };

    public static MapManagerState FromRequest(UpdateMap request)
        => new()
        {
            IsMapReady = false,
            MapId = request.MapId
        };

    public static MapManagerState FromRequest(ResetMap request)
        => new()
        {
            IsMapReady = false,
            MapId = request.MapId
        };

    public MapManagerState() { }

    public Guid MapId { get; internal set; }
    public bool IsMapReady { get; internal set; } = false;
}
