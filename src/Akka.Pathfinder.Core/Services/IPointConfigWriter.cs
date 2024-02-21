using Akka.Pathfinder.Core.Configs;
using MongoDB.Driver;

namespace Akka.Pathfinder.Core;

public interface IPointConfigWriter : IPointConfigReader
{
    public Task AddPointConfigsAsync(Guid collectionId, List<PointConfig> pointConfigs, CancellationToken cancellationToken = default);
    public Task AddPointConfigAsync(Guid collectionId, PointConfig pointConfig, CancellationToken cancellationToken = default);
}

public class PointConfigWriter : PointConfigReader, IPointConfigWriter
{
    public PointConfigWriter(IMongoDatabase database) : base(database)
    { }

    public async Task AddPointConfigsAsync(Guid collectionId, List<PointConfig> pointConfigs, CancellationToken cancellationToken = default)
    {
        var collection = Database.GetCollection<PointConfig>(collectionId.ToString());

        await collection.CreateIndexAsync(builder => builder.Ascending(item => item.Id), $"PointConfig_{collectionId}_Id", cancellationToken);
        await collection.InsertManyAsync(pointConfigs, new InsertManyOptions() { IsOrdered = true }, cancellationToken);
    }

    public async Task AddPointConfigAsync(Guid collectionId, PointConfig pointConfig, CancellationToken cancellationToken = default)
    {
        var collection = Database.GetCollection<PointConfig>(collectionId.ToString());

        await collection.CreateIndexAsync(builder => builder.Ascending(item => item.Id), $"PointConfig_{collectionId}_Id", cancellationToken);
        await collection.InsertOneAsync(pointConfig, cancellationToken: cancellationToken);
    }
}