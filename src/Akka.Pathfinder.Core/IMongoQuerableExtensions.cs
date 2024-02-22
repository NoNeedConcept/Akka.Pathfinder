using MongoDB.Driver;

namespace Akka.Pathfinder.Core;

public static class IMongoQueryableExtensions
{
    public static void CreateIndex<T>(this IMongoCollection<T> collection, Func<IndexKeysDefinitionBuilder<T>, IndexKeysDefinition<T>> selector, string indexName)
    {
        var indexKeysDefinition = selector.Invoke(Builders<T>.IndexKeys);
        collection.Indexes.CreateOne(new CreateIndexModel<T>(indexKeysDefinition, new CreateIndexOptions {  Name = indexName,  }));
    }
}
