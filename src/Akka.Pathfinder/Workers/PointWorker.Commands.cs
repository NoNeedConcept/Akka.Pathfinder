using System.Diagnostics;
using Akka.Pathfinder.Core.Messages;
using Akka.Pathfinder.Core.States;
using Akka.Pathfinder.Core;
using Akka.Persistence;
using Akka.Actor;
using Akka.Pathfinder.Core.Persistence;
using moin.akka.endpoint;

namespace Akka.Pathfinder.Workers;

public partial class PointWorker
{
    private void PathfinderDeactivatedHandler(PathfinderDeactivated msg)
    {
        using var activity = Telemetry.ActivitySource.StartActivity(msg.GetType().Name)?.SetTag("EntityId", _entityId);
        _logger.Verbose("[{PointId}][{MessageType}][{PathfinderId}] received", _entityId, msg.GetType().Name,
            msg.PathfinderId);
        _state.AddInactivePathfinder(msg.PathfinderId);
        _state.RemovePathfinderPathCost(msg.PathfinderId);
        _state.RemoveOldPathfinderIds(TimeSpan.FromMinutes(10));
    }

    private void LocalPointConfigHandler(LocalPointConfig msg)
    {
        using var activity = Telemetry.ActivitySource.StartActivity(msg.GetType().Name)?.SetTag("EntityId", _entityId);
        _logger.Verbose("[{PointId}][{MessageType}] received", _entityId, msg.GetType().Name);
        if (msg is LocalPointConfigFailed item)
        {
            _logger.Error(item.Exception, "[{PointId}]", _entityId);
        }
        else if (msg is LocalPointConfigSuccess success)
        {
            _state = PointWorkerState.FromConfig(success.Config!, _state?.State);
            Become(Ready);
        }
    }

    private void InitializePointHandler(InitializePoint msg)
    {
        using var activity = Telemetry.ActivitySource.StartActivity(msg.GetType().Name)?.SetTag("EntityId", _entityId);
        _logger.Verbose("[{PointId}][{MessageType}] received", _entityId, msg.GetType().Name);
        _state = PointWorkerState.FromInitialize(msg.PointId, msg.CollectionId);
        Persist(new PersistedInitializedPointState(msg.PointId, msg.CollectionId), _ => { });
        Sender.Tell(new PointInitialized(msg.RequestId, msg.PointId));
        Become(Configure);
    }

    private void UpdatePointDirectionHandler(UpdatePointDirection msg)
    {
        using var activity = Telemetry.ActivitySource.StartActivity(msg.GetType().Name)?.SetTag("EntityId", _entityId);
        _logger.Verbose("[{PointId}][{MessageType}] received", _entityId, msg.GetType().Name);
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
        using var activity = Telemetry.ActivitySource.StartActivity(msg.GetType().Name)?.SetTag("EntityId", _entityId);
        _logger.Verbose("[{PointId}][{MessageType}] received", _entityId, msg.GetType().Name);
        Become(Configure);
        Sender.Tell(new PointReloaded(msg.RequestId, msg.PointId));
    }

    private void CostRequestHandler(CostRequest msg)
    {
        using var activity = Telemetry.ActivitySource.StartActivity(msg.GetType().Name)?.SetTag("EntityId", _entityId);
        _logger.Verbose("[{PointId}][{MessageType}] received", _entityId, msg.GetType().Name);

        var success = msg switch
        {
            PointCostRequest value => _state.ChangePointCost(value.Value, value.ChangeMethod),
            DirectionCostRequest value => _state.ChangeDirectionCost(value.Value, value.Direction, value.ChangeMethod),
            _ => throw new NotImplementedException(),
        };

        Sender.Tell(new UpdateCostResponse(msg.RequestId, msg.PointId, success));
    }

    private void PointCommandRequestHandler(PointCommandRequest msg)
    {
        using var activity = Telemetry.ActivitySource.StartActivity(msg.GetType().Name)?.SetTag("EntityId", _entityId);
        _logger.Verbose("[{PointId}][{MessageType}] received", _entityId, msg.GetType().Name);

        _ = msg switch
        {
            BlockPointCommandRequest value when value.PointId == _state.PointId => _state.Block(),
            UnblockPointCommandRequest value when value.PointId == _state.PointId => _state.Unblock(),
            _ => false,
        };
    }

    private void FindPathRequestHandler(FindPathRequest msg)
    {
        using var activity = Telemetry.ActivitySource.StartActivity(msg.GetType().Name)?.SetTag("EntityId", _entityId);
        _logger.Verbose("[{PointId}][{MessageType}] received", _entityId, msg.GetType().Name);

        if (_state.TryIsInactivePathfinder(msg.PathfinderId)) return;

        if (_state.IsBlockedAndGetResponse(msg, out var value))
        {
            Sender.Tell(value, ActorRefs.NoSender);
            return;
        }

        if (_state.TryLoopDetection(msg))
        {
            _logger.Warning("[{PointId}][{PathfinderId}] LoopDetection", _entityId, msg.PathfinderId);
            return;
        }

        if (_state.TryAddCurrentPointCost(msg, out var newRequest)) return;

        if (_state.TryIsNotShortestPathForPathfinderId(newRequest)) return;

        if (_state.TryIsArrivedTargetPoint(newRequest, PersistPath, out var pathFound))
        {
            Sender.Tell(pathFound, ActorRefs.NoSender);
            return;
        }

        var pointWorkerClient = Context.System.GetRegistry().GetClient<Endpoint.PointWorker>();
        var items = _state.GetAllForwardMessages(newRequest);
        foreach (var item in items)
        {
            pointWorkerClient.Forward(item);
        }
    }

    private void SaveSnapshotFailureHandler(SaveSnapshotFailure msg)
        => _logger.Error(msg.Cause, "[{PointId}][SNAPSHOTFAILURE][{SequenceNr}]", _entityId, msg.Metadata.SequenceNr);

    private void SaveSnapshotSuccessHandler(SaveSnapshotSuccess msg)
        => _logger.Verbose("[{PointId}][SNAPSHOTSUCCESS][{SequenceNr}]", _entityId, msg.Metadata.SequenceNr);
}