using Akka.Pathfinder.Core.Persistence;
using Akka.Pathfinder.Core.States;
using Akka.Persistence;

namespace Akka.Pathfinder.Workers;

public partial class PathfinderWorker
{
    public void RecoverSnapshotOffer(SnapshotOffer msg)
    {
        try
        {
            _logger.Information("[{PathfinderId}][RECOVER][{SequenceNr}]",
                    _entityId, msg.Metadata.SequenceNr);

            if (msg.Snapshot is PersistedPathfinderWorkerState persisted)
            {
                _state = PathfinderWorkerState.FromSnapshot(persisted);
                return;
            }

            _logger.Warning("[{PathfinderId}][RECOVER][{MessageType}] Invalid snapshot type!", _entityId, msg.Snapshot.GetType().Name);
        }
        catch (Exception ex)
        {
            _logger.Fatal(ex, "[{PathfinderId}][RECOVER][{MessageType}] Failed to recover", _entityId, msg.GetType().Name);
            Become(Failure);
        }
    }
}
