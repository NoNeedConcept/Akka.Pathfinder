using Akka.Actor;
using Akka.Pathfinder.Core.Messages;

namespace Akka.Pathfinder.Managers;

public partial class SenderManager : ReceiveActor
{
    private readonly Serilog.ILogger _logger;
    private readonly Dictionary<Guid, IActorRef> _pathfinderSender = [];

    public SenderManager()
    {
        _logger = Serilog.Log.Logger.ForContext("SourceContext", GetType().Name);
        Receive<SavePathfinderSender>(SavePathfinderSenderHandler);
        Receive<ForwardToPathfinderSender>(ForwardToPathfinderSenderHandler);
    }
}