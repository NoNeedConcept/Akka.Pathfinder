using Akka.Pathfinder.Core.Messages;
using Akka.Pathfinder.Core.States;
using Akka.Pathfinder.Core;
using Akka.Persistence;
using Akka.Actor;
using Akka.Pathfinder.Core.Persistence;

namespace Akka.Pathfinder.Workers;

public partial class PointWorker
{
    private void PathfinderDeactivatedHandler(PathfinderDeactivated msg)
    {
        _logger.Verbose("[{PointId}][{MessageType}][{PathfinderId}] received", EntityId, msg.GetType().Name, msg.PathfinderId);
        _state.AddInactivePathfinder(msg.PathfinderId);
        _state.RemovePathfinderPathCost(msg.PathfinderId);
        _state.RemoveOldPathfinderIds(TimeSpan.FromMinutes(10));
    }

    private void LocalPointConfigHandler(LocalPointConfig msg)
    {
        _logger.Verbose("[{PointId}][{MessageType}] received", EntityId, msg.GetType().Name);
        if (msg is LocalPointConfigFailed item)
        {
            _logger.Error(item.Exception, "[{PointId}]", EntityId);
        }
        else if (msg is LocalPointConfigSuccess success)
        {
            _state = PointWorkerState.FromConfig(success.Config!, _state?.State);
            PersistState();
            Become(Ready);
        }
    }

    private void InitializePointHandler(InitializePoint msg)
    {
        _logger.Verbose("[{PointId}][{MessageType}] received", EntityId, msg.GetType().Name);
        _state = PointWorkerState.FromInitialize(msg.PointId, msg.CollectionId);
        Persist(new PersistedInitializedPointState(msg.PointId, msg.CollectionId), _ => { });
        Sender.Tell(new PointInitialized(msg.RequestId, msg.PointId));
        Become(Configure);
    }

    private void UpdatePointDirectionHandler(UpdatePointDirection msg)
    {
        _logger.Verbose("[{PointId}][{MessageType}] received", EntityId, msg.GetType().Name);
        Become(Update);
        var updatedConfig = msg.Config with
        {
            DirectionConfigs = _state.MergeDirectionConfigs(msg.Config.DirectionConfigs)
        };

        Sender.Tell(new PointDirectionUpdated(msg.RequestId, msg.Config.Id));
        Self.Forward(new LocalPointConfigSuccess(updatedConfig));
    }

    private void ReloadPointHandler(ReloadPoint msg)
    {
        _logger.Verbose("[{PointId}][{MessageType}] received", EntityId, msg.GetType().Name);
        Become(Configure);
        Sender.Tell(new PointReloaded(msg.RequestId, msg.PointId));
    }

    private void CostRequestHandler(CostRequest msg)
    {
        _logger.Verbose("[{PointId}][{MessageType}] received", EntityId, msg.GetType().Name);

        var success = msg switch
        {
            PointCostRequest value => _state.ChangePointCost(value.Value, value.ChangeMethod),
            DirectionCostRequest value => _state.ChangeDirectionCost(value.Value, value.Direction, value.ChangeMethod),
            _ => throw new NotImplementedException(),
        };

        Sender.Tell(new UpdateCostResponse(msg.RequestId, msg.PointId, success));
        PersistState();
    }

    private void PointCommandRequestHandler(PointCommandRequest msg)
    {
        _logger.Verbose("[{PointId}][{MessageType}] received", EntityId, msg.GetType().Name);

        _ = msg switch
        {
            BlockPointCommandRequest value when value.PointId == _state.PointId => _state.Block(),
            UnblockPointCommandRequest value when value.PointId == _state.PointId => _state.Unblock(),
            _ => false,
        };
    }

    private void FindPathRequestHandler(FindPathRequest msg)
    {
        _logger.Verbose("[{PointId}][{MessageType}] received", EntityId, msg.GetType().Name);

        if (_state.TryIsInactivePathfinder(msg.PathfinderId)) return;

        if (_state.IsBlockedAndGetResponse(msg, out PathFound value))
        {
            Sender.Tell(value, ActorRefs.NoSender);
            return;
        }

        if (_state.TryLoopDetection(msg))
        {
            _logger.Warning("[{PointId}][{PathfinderId}] LoopDetection", EntityId, msg.PathfinderId);
            return;
        }

        if (_state.TryAddCurrentPointCost(msg, out FindPathRequest newRequest)) return;

        if (_state.TryIsNotShortestPathForPathfinderId(newRequest)) return;

        if (_state.TryIsArrivedTargetPoint(newRequest, PersistPath, out PathFound pathFound))
        {
            Sender.Tell(pathFound, ActorRefs.NoSender);
            return;
        }

        var pointWorkerClient = Context.System.GetRegistry().Get<PointWorkerProxy>();
        var items = _state.GetAllForwardMessages(newRequest);
        foreach (var item in items)
        {
            pointWorkerClient.Forward(item);
        }

    }

    private void SaveSnapshotFailureHandler(SaveSnapshotFailure msg)
        => _logger.Error(msg.Cause, "[{PointId}][SNAPSHOTFAILURE][{SequenceNr}]", EntityId, msg.Metadata.SequenceNr);

    private void SaveSnapshotSuccessHandler(SaveSnapshotSuccess msg)
        => _logger.Verbose("[{PointId}][SNAPSHOTSUCCESS][{SequenceNr}]", EntityId, msg.Metadata.SequenceNr);
}