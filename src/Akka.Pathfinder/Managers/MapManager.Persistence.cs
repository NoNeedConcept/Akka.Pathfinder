namespace Akka.Pathfinder.Managers;

public partial class MapManager
{
    private void PersistState()
    {
        var persistedWorkerState = _state.GetPersistenceState();
        SaveSnapshot(persistedWorkerState);
    }
}