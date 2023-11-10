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
            _logger.Debug("[{PathfinderId}][RECOVER][SnapshotOffer<{SnapshotType}>][{SequenceNr}]",
                    EntityId, msg.Snapshot.GetType().Name, msg.Metadata.SequenceNr);

            if (msg.Snapshot is PersistedPathfinderWorkerState persisted)
            {
                _state = PathfinderWorkerState.FromSnapshot(persisted);
                return;
            }

            _logger.Warning("[{PathfinderId}][RECOVER][{MessageType}] Invalid snapshot type!", EntityId, msg.Snapshot.GetType().Name);
        }
        catch (Exception ex)
        {
            _logger.Fatal(ex, "[{PathfinderId}][RECOVER][{MessageType}] Failed to recover", EntityId, msg.GetType().Name);
            Become(Failure);
        }
    }
}
