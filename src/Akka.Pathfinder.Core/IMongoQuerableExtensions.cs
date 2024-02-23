using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Akka.Pathfinder.Core;

public static class IMongoQueryableExtensions
{
    public static async Task ThrottleAsync<T>(this IQueryable<T> values, Action<T> action, TimeSpan? initialDelay = null, TimeSpan? interval = null)
    {
        initialDelay ??= TimeSpan.Zero;
        interval ??= TimeSpan.FromMicroseconds(5);

        await Task.Delay(initialDelay.Value);
        var results = await values.ToListAsync();
        foreach (var item in results)
        {
            await Task.Delay(interval.Value);
            action.Invoke(item);
        }
    }

    public static void CreateIndex<T>(this IMongoCollection<T> collection, Func<IndexKeysDefinitionBuilder<T>, IndexKeysDefinition<T>> selector, string indexName)
    {
        var indexKeysDefinition = selector.Invoke(Builders<T>.IndexKeys);
        collection.Indexes.CreateOne(new CreateIndexModel<T>(indexKeysDefinition, new CreateIndexOptions {  Name = indexName,  }));
    }
}
