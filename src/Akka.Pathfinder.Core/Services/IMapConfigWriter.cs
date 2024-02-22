using Akka.Pathfinder.Core.Configs;
using MongoDB.Driver;

namespace Akka.Pathfinder.Core.Services;

public interface IMapConfigWriter : IMapConfigReader
{
    Task<bool> WriteAsync(MapConfig config, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid mapId, CancellationToken cancellationToken = default);
}

public class MapConfigWriter : MapConfigReader, IMapConfigWriter
{
    public MapConfigWriter(IMongoCollection<MapConfig> collection) : base(collection)
    { }

    public async Task<bool> WriteAsync(MapConfig config, CancellationToken cancellationToken = default)
    {
        var update = Builders<MapConfig>.Update
        .Set(x => x.Id, config.Id)
        .Set(x => x.CollectionIds, config.CollectionIds)
        .Set(x => x.Count, config.Count);

        var result = await Collection.UpdateOneAsync(x => x.Id == config.Id, update, new UpdateOptions { IsUpsert = true }, cancellationToken);
        return result.IsAcknowledged;
    }

    public async Task<bool> DeleteAsync(Guid mapId, CancellationToken cancellationToken = default)
    {
        var result = await Collection.DeleteOneAsync(x => x.Id == mapId, cancellationToken);
        return result.IsAcknowledged && result.DeletedCount > 0;
    }
}
