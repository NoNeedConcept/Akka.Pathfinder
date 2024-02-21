﻿using Akka.Pathfinder.Core.Persistence;
using Akka.Pathfinder.Core.States;
using Akka.Persistence;

namespace Akka.Pathfinder.Workers;

public partial class PointWorker
{
    public void RecoverSnapshotOffer(SnapshotOffer msg)
    {
        try
        {
            _logger.Verbose("[{PointId}][RECOVER][{SequenceNr}]",
                    EntityId, msg.Metadata.SequenceNr);

            if (msg.Snapshot is PersistedPointWorkerState persisted)
            {
                _state = PointWorkerState.FromSnapshot(persisted);
                return;
            }

            _logger.Warning("[{PointId}][RECOVER] Invalid snapshot type!", EntityId);
        }
        catch (Exception ex)
        {
            _logger.Fatal(ex, "[{PointId}][RECOVER] Failed to recover", EntityId);
            Become(Failure);
        }
    }
}
