using Akka.Pathfinder.Core.Configs;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Akka.Pathfinder.Core.Services;

public interface IMapConfigReader
{
    IQueryable<MapConfig> Get();
    MapConfig? Get(Guid mapId);
}

public class MapConfigReader : IMapConfigReader
{
    public MapConfigReader(IMongoCollection<MapConfig> collection)
        => Collection = collection;

    protected IMongoCollection<MapConfig> Collection { get; }

    public IQueryable<MapConfig> Get()
        => Collection.AsQueryable();

    public MapConfig? Get(Guid mapId)
        => Get().Where(x => x.Id == mapId).SingleOrDefault();
}