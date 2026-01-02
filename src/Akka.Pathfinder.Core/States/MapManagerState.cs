using Akka.Pathfinder.Core.Persistence;

namespace Akka.Pathfinder.Core.States;

public class MapManagerState
{
    public static MapManagerState FromRequest(Guid mapId)
        => new()
        {
            IsMapReady = false,
            MapId = mapId
        };

    public static MapManagerState FromSnapshot(PersistedMapManagerState msg)
        => new()
        {
            MapId = msg.MapId,
            IsMapReady = msg.IsMapReady,
        };

    public Guid MapId { get; private set; }
    public bool IsMapReady { get; private set; }
    public void SetMapIsReady() => IsMapReady = true;

    public PersistedMapManagerState GetPersistenceState() => new(MapId, IsMapReady);
}