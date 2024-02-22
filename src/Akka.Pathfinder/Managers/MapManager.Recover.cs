using Akka.Pathfinder.Core.Persistence;
using Akka.Pathfinder.Core.States;
using Akka.Persistence;

namespace Akka.Pathfinder.Managers;

public partial class MapManager
{
    private void RecoverSnapshotOffer(SnapshotOffer msg)
    {
        try
        {
            _logger.Verbose("[MapManager][RECOVER][{SequenceNr}]",
                msg.Metadata.SequenceNr);

            if (msg.Snapshot is PersistedMapManagerState persisted)
            {
                _state = MapManagerState.FromSnapshot(persisted);
                return;
            }

            _logger.Warning("[MapManager][RECOVER] Invalid snapshot type!");
        }
        catch (Exception ex)
        {
            _logger.Fatal(ex, "[MapManager][RECOVER] Failed to recover");
        }
    }
}