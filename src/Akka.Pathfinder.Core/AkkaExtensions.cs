using Akka.Actor;
using Akka.DependencyInjection;
using Akka.Hosting;

namespace Akka.Pathfinder.Core;

public static class AkkaExtensions
{
    public static IReadOnlyActorRegistry GetRegistry(this ActorSystem system) => ActorRegistry.For(system);

    public static IReadOnlyActorRegistry GetRegistry(this IUntypedActorContext context) => context.System.GetRegistry();

    public static IActorRef Get<T>(this IReadOnlyActorRegistry registry)
    {
        if (registry.TryGet<T>(out var actor))
            return actor;

        throw new MissingActorRegistryEntryException("No actor registered for key " + typeof(T).FullName);
    }

    /// <summary>
    /// </summary>
    /// <param name="registry"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    /// <exception cref="MissingActorRegistryEntryException"></exception>
    public static IActorRef Get(this IReadOnlyActorRegistry registry, Type type)
    {
        if (registry.TryGet(type, out var actor))
            return actor;

        throw new MissingActorRegistryEntryException("No actor registered for key " + type.FullName);
    }

    public static IActorRef Props<T>(this ActorSystem system, string? name = null, params object[] args) where T : ActorBase
        => system.ActorOf(DependencyResolver.For(system).Props<T>(args), name);
}
