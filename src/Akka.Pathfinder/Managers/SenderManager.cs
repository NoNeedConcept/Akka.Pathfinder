using Akka.Actor;
using Akka.Pathfinder.Core.Messages;
using Servus.Core.Diagnostics;

namespace Akka.Pathfinder.Managers;


[ActivitySourceName("Pathfinder")]
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