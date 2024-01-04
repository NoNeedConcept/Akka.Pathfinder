using Akka.Pathfinder.Core.Messages;
using Akka.Pathfinder.Core.States;
using System.Reactive.Linq;
using Akka.Pathfinder.Core;
using Akka.Persistence;
using Akka.Actor;

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
        _state = PointWorkerState.FromConfig(msg.Config, _state?.State);
        PersistState();
        Become(Ready);
    }

    private void InitializePointHandler(InitializePoint msg)
    {
        _logger.Verbose("[{PointId}][{MessageType}] received", EntityId, msg.GetType().Name);
        _state = PointWorkerState.FromInitialize(msg.PointId, msg.CollectionId);
        PersistState();
        Become(Configure);
    }

    private void UpdatePointDirectionHandler(UpdatePointDirection msg)
    {
        _logger.Verbose("[{PointId}][{MessageType}] received", EntityId, msg.GetType().Name);
        Become(Configure);
        var updatedConfig = msg.Config with
        {
            DirectionConfigs = _state.MergeDirectionConfigs(msg.Config.DirectionConfigs)
        };

        Self.Forward(new LocalPointConfig(updatedConfig));
    }

    private void ResetPointHandler(ResetPoint msg)
    {
        _logger.Verbose("[{PointId}][{MessageType}] received", EntityId, msg.GetType().Name);
        Become(Configure);
        Self.Forward(new LocalPointConfig(msg.Config));
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

        Sender.Tell(new UpdateCostResponse(msg.PointId, success));
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

    private async Task FindPathRequestHandler(FindPathRequest msg)
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
            _logger.Debug("[{PointId}] LoopDetection", EntityId);
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
        await _state
        .GetAllForwardMessages(newRequest)
        .Throttle(pointWorkerClient.Forward, TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1));
    }

    private void SaveSnapshotFailureHandler(SaveSnapshotFailure msg)
        => _logger.Error("[{PointId}] failed to create snapshot [{SequenceNr}]", EntityId, msg.Metadata.SequenceNr);

    private void SaveSnapshotSuccessHandler(SaveSnapshotSuccess msg)
        => _logger.Information("[{PointId}] successfully create snapshot [{SequenceNr}]", EntityId, msg.Metadata.SequenceNr);
}
