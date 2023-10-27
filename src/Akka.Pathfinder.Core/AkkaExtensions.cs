using Akka.Actor;
using Akka.Hosting;

namespace Akka.Pathfinder.Core;

public static class AkkaExtensions
{
    public static IReadOnlyActorRegistry GetRegistry(this ActorSystem system) => ActorRegistry.For(system);

    public static IActorRef Get<T>(this IReadOnlyActorRegistry registry)
    {
        if (registry.TryGet<T>(out var actor))
            return actor;

        throw new MissingActorRegistryEntryException("No actor registered for key " + typeof(T).FullName);
    }
}
