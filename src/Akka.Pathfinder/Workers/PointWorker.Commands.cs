using System.Reactive.Linq;
using Akka.Actor;
using Akka.Pathfinder.Core;
using Akka.Pathfinder.Core.Messages;
using Akka.Pathfinder.Core.States;
using Akka.Persistence;

namespace Akka.Pathfinder.Workers;

public partial class PointWorker
{
    private void PathfinderDeactivatedHandler(PathfinderDeactivated msg)
    {
        _logger.Debug("[{PointId}][{MessageType}] received", EntityId, msg.GetType().Name);
        _state.AddInactivePathfinder(msg);
        _state.RemoveOldPathfinderIds(TimeSpan.FromMinutes(10));
    }

    private void LocalPointConfigHandler(LocalPointConfig msg)
    {
        _logger.Debug("[{PointId}][{MessageType}] received", EntityId, msg.GetType().Name);
        _state = PointWorkerState.FromConfig(msg.Config, _state?.State);
        PersistState();
        Context.System.EventStream.Publish(new PointInitialized(msg.Config.Id));
        Become(Ready);
    }

    private void InitializePointHandler(InitializePoint msg)
    {
        Become(Configure);
        _logger.Debug("[{PointId}][{MessageType}] received", EntityId, msg.GetType().Name);
        Self.Forward(new LocalPointConfig(msg.Config));
    }

    private void UpdatePointDirectionHandler(UpdatePointDirection msg)
    {
        Become(Configure);
        _logger.Debug("[{PointId}][{MessageType}] received", EntityId, msg.GetType().Name);
        var updatedConfig = msg.Config with
        {
            DirectionConfigs = _state.MergeDirectionConfigs(msg.Config.DirectionConfigs)
        };

        Self.Forward(new LocalPointConfig(updatedConfig));
    }

    private void ResetPointHandler(ResetPoint msg)
    {
        Become(Configure);
        _logger.Debug("[{PointId}][{MessageType}] received", EntityId, msg.GetType().Name);
        Self.Forward(new LocalPointConfig(msg.Config));
    }

    private void CostRequestHandler(CostRequest msg)
    {
        _logger.Debug("[{PointId}][{MessageType}] received", EntityId, msg.GetType().Name);

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
        _logger.Debug("[{PointId}][{MessageType}] received", EntityId, msg.GetType().Name);

        _ = msg switch
        {
            BlockPointCommandRequest value when value.PointId == _state.PointId => _state.Block(),
            UnblockPointCommandRequest value when value.PointId == _state.PointId => _state.Unblock(),
            _ => false,
        };
    }

    private void CreatePathPointRequestPathHandler(FindPathRequest msg)
    {
        _logger.Verbose("[{PointId}][{MessageType}] received", EntityId, msg.GetType().Name);

        if (_state.TryIsInactivePathfinder(msg.PathfinderId)) return;

        if (_state.IsBlockedAndGetResponse(msg, out PathFound value))
        {
            Sender.Tell(value, ActorRefs.NoSender);
            return;
        }

        if (_state.TryLoopDetection(msg, out PathFound response))
        {
            Sender.Tell(response, ActorRefs.NoSender);
            return;
        }

        if (_state.TryAddCurrentPointCost(msg, out FindPathRequest newRequest))
        {
            return;
        }

        if (_state.TryIsArrivedTargetPoint(newRequest, PersistPath, out PathFound pathFound))
        {
            Sender.Tell(pathFound, ActorRefs.NoSender);
            return;
        }

        if (_state.TrySavePartialPath(newRequest, PersistPath, out FindPathRequest newFindPathRequest))
        {
            _logger.Debug("[{PointId}][{MessageType}] not saved :(", EntityId, msg.GetType().Name);
        }

        _ = _state
        .GetAllForwardMessages(newFindPathRequest)
        .Throttle(
            msg => Context.System.GetRegistry().Get<PointWorkerProxy>().Forward(msg),
            TimeSpan.FromMilliseconds(5),
            TimeSpan.FromMilliseconds(2));
    }

    private void SaveSnapshotFailureHandler(SaveSnapshotFailure msg)
        => _logger.Error("[{PointId}] failed to create snapshot [{SequenceNr}]",
                msg.Metadata.PersistenceId, msg.Metadata.SequenceNr);

    private void SaveSnapshotSuccessHandler(SaveSnapshotSuccess msg)
        => _logger.Information("[{PointId}] successfully create snapshot [{SequenceNr}]",
                msg.Metadata.PersistenceId, msg.Metadata.SequenceNr);
}
