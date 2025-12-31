using Akka.Actor;
using Akka.Pathfinder.Core.Messages;
using Servus.Akka.Diagnostics;

namespace Akka.Pathfinder.Managers;

public partial class SenderManager : TracedMessageActor
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