using Akka.Pathfinder.Core.Configs;
using MongoDB.Driver;

namespace Akka.Pathfinder.Core;

public interface IMapConfigReader
{
    IQueryable<MapConfig> Get();
    MapConfig Get(Guid MapId);
}

public class MapConfigReader : IMapConfigReader
{
    protected IMongoCollection<MapConfig> Collection { get; init; }
    public MapConfigReader(IMongoCollection<MapConfig> collection) 
        => Collection = collection;

    public IQueryable<MapConfig> Get()
        => Collection.AsQueryable();

    public MapConfig Get(Guid MapId)
        => Get().Single(x => x.Id == MapId);
}