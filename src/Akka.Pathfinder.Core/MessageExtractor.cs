using Akka.Cluster.Sharding;

namespace Akka.Pathfinder.Core;

public class MessageExtractor : HashCodeMessageExtractor
{
    public MessageExtractor() : base(1500) { }

    public override string EntityId(object message)
    {
        return message is IEntityId ntt ? ntt.EntityId : throw new ArgumentException("DUMMKOPF");
    }
}