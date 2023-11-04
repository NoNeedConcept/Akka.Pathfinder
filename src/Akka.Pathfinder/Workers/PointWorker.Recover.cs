using Akka.Pathfinder.Core.Configs;
using Akka.Pathfinder.Core.Persistence;
using Akka.Pathfinder.Core.States;
using Akka.Persistence;

namespace Akka.Pathfinder.Workers;

public partial class PointWorker
{
    public void RecoverSnapshotOffer(SnapshotOffer msg)
    {
        try
        {
            _logger.Debug("[{PointId}][RECOVER][SnapshotOffer<{SnapshotType}>][{SequenceNr}]",
                    EntityId, msg.Snapshot.GetType().Name, msg.Metadata.SequenceNr);

            if (msg.Snapshot is PersistedPointWorkerState persisted)
            {
                _state = PointWorkerState.FromSnapshot(persisted);
                return;
            }

            _logger.Warning("[{PointId}][RECOVER][{MessageType}] Invalid snapshot type!", EntityId, msg.Snapshot.GetType().Name);
        }
        catch (Exception ex)
        {
            _logger.Fatal(ex, "[{PointId}][RECOVER][{MessageType}] Failed to recover", EntityId, msg.GetType().Name);
            Become(Failure);
        }
    }

    public void RecoverPointConfig(PointConfig msg)
    {
        try
        {
            _logger.Debug("[{PointId}][RECOVER][{MessageType}]", EntityId, msg.GetType().Name);
            _state = PointWorkerState.FromConfig(msg);
        }
        catch (Exception ex)
        {
            _logger.Fatal(ex, "[{PointId}][RECOVER][{MessageType}] Failed to recover", EntityId, msg.GetType().Name);
            Become(Failure);
        }
    }
}
