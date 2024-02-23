using Akka.Pathfinder.Core.Configs;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Akka.Pathfinder.Core;

public interface IMapConfigReader
{
    IQueryable<MapConfig> Get();
    Task<MapConfig> GetAsync(Guid mapId, CancellationToken cancellationToken = default);
}

public class MapConfigReader : IMapConfigReader
{
    public MapConfigReader(IMongoCollection<MapConfig> collection)
        => Collection = collection;

    protected IMongoCollection<MapConfig> Collection { get; }

    public IQueryable<MapConfig> Get()
        => Collection.AsQueryable();

    public Task<MapConfig> GetAsync(Guid mapId, CancellationToken cancellationToken = default)
        => Get().Where(x => x.Id == mapId).SingleOrDefaultAsync(cancellationToken);
}