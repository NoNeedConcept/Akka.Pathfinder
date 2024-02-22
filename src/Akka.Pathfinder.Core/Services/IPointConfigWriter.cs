using Akka.Pathfinder.Core.Configs;
using MongoDB.Driver;

namespace Akka.Pathfinder.Core.Services;

public interface IPointConfigWriter : IPointConfigReader
{
    public Task AddPointConfigsAsync(Guid collectionId, List<PointConfig> pointConfigs, CancellationToken cancellationToken = default);
    public Task AddPointConfigAsync(Guid collectionId, PointConfig pointConfig, CancellationToken cancellationToken = default);
    public Task DeleteCollectionAsync(Guid collectionId, CancellationToken cancellationToken = default);
}

public class PointConfigWriter : PointConfigReader, IPointConfigWriter
{
    public PointConfigWriter(IMongoDatabase database) : base(database)
    { }

    public async Task AddPointConfigsAsync(Guid collectionId, List<PointConfig> pointConfigs, CancellationToken cancellationToken = default)
    {
        var collection = Database.GetCollection<PointConfig>(collectionId.ToString());

        collection.CreateIndex(builder => builder.Ascending(item => item.Id), $"PointConfig_{collectionId}_Id");
        await collection.InsertManyAsync(pointConfigs, new InsertManyOptions { IsOrdered = true }, cancellationToken);
    }

    public async Task AddPointConfigAsync(Guid collectionId, PointConfig pointConfig, CancellationToken cancellationToken = default)
    {
        var collection = Database.GetCollection<PointConfig>(collectionId.ToString());

        collection.CreateIndex(builder => builder.Ascending(item => item.Id), $"PointConfig_{collectionId}_Id");
        await collection.InsertOneAsync(pointConfig, cancellationToken: cancellationToken);
    }

    public async Task DeleteCollectionAsync(Guid collectionId, CancellationToken cancellationToken = default)
    {
        await Database.DropCollectionAsync(collectionId.ToString(), cancellationToken);
    }
}