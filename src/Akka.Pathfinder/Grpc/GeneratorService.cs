using Akka.Pathfinder.Core;
using Akka.Pathfinder.Core.Configs;
using Akka.Pathfinder.Grpc;
using Akka.Pathfinder.Layout;
using Grpc.Core;

namespace Akka.Pathfinder;

public class GeneratorService(IMapConfigWriter mapConfigWriter, IPointConfigWriter pointConfigWriter) : Generator.GeneratorBase
{
    private readonly IMapConfigWriter _mapConfigWriter = mapConfigWriter;
    private readonly IPointConfigWriter _pointConfigWriter = pointConfigWriter;

    public override async Task<Ack> CreateMap(Grpc.MapSettings request, ServerCallContext context)
    {
        var mapfactory = MapFactoryProvider.Instance.CreateFactory();
        var mapConfig = mapfactory.Create(request.To());

        await _mapConfigWriter.WriteAsync(new MapConfig(mapConfig.Id, mapConfig.CollectionIds, mapConfig.Count));
        foreach (var (key, value) in mapConfig.Configs)
        {
            await _pointConfigWriter.AddPointConfigsAsync(key, value);
        }

        return new Ack { Success = true };
    }
}
