using Akka.Cluster.Sharding;

namespace Akka.Pathfinder.Core;

public class MessageExtractor : HashCodeMessageExtractor
{
    public MessageExtractor(int maxShard = 150) : base(maxShard) { }

    public override string EntityId(object message)
        => message is IEntityId ntt ? ntt.EntityId : throw new ArgumentException("DUMMKOPF");
}