using Akka.Pathfinder.Core.Messages;

namespace Akka.Pathfinder.Workers;

public partial class PointWorker
{
    private void Initialize()
    {
        _logger.Debug("[{PointId}][INITIALIZE] START", EntityId);

        if (_state?.Initialize == true)
        {
            Become(Ready);
            return;
        }

        Command<InitializePoint>(InitPointHandler);

        CommandAny(msg => Stash.Stash());
    }

    private void Failure()
    {
        _logger.Debug("[{PointId}][FAILURE]", EntityId);
        CommandAny(msg =>
        {
            _logger.Debug("[{PointId}][{MessageType}] message received -> no action in failure state", EntityId, msg.GetType().Name);
        });
    }

    private void Ready()
    {
        _logger.Debug("[{PointId}][READY]", EntityId);

        PersistState();
        OnReady();

        Stash.UnstashAll();
    }
}
