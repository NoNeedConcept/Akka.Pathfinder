using Akka.Pathfinder.AcceptanceTests.Drivers;
using Reqnroll;

namespace Akka.Pathfinder.AcceptanceTests.StepDefinitions;

[Binding]
public class RedisSteps
{
    private readonly RedisDriver _redisDriver;
    private readonly DatabaseDriver _databaseDriver;
    private readonly Serilog.ILogger _logger = Serilog.Log.Logger.ForContext<RedisSteps>();

    public RedisSteps(RedisDriver redisDriver, DatabaseDriver databaseDriver)
    {
        _redisDriver = redisDriver;
        _databaseDriver = databaseDriver;
    }

    [Then(@"Redis should contain journal entries for PathfinderId (.*)")]
    public async Task ThenRedisShouldContainJournalEntriesForPathfinderId(string pathfinderId)
    {
        _logger.Information("[TEST][RedisSteps] Checking Redis for journal entries for {PathfinderId}", pathfinderId);

        var allKeys = await _redisDriver.GetAllKeysAsync();
        var matchingKeys = allKeys.Where(k => k.Contains(pathfinderId, StringComparison.OrdinalIgnoreCase)).ToList();
        
        Assert.NotEmpty(matchingKeys);
    }

    [Then(@"MongoDB should not contain journal entries for PathfinderId (.*)")]
    public async Task ThenMongoDbShouldNotContainJournalEntriesForPathfinderId(string pathfinderId)
    {
        _logger.Information("[TEST][RedisSteps] Checking MongoDB for journal entries for {PathfinderId}", pathfinderId);
        var hasEntries = await _databaseDriver.HasJournalEntriesAsync(pathfinderId);
        Assert.False(hasEntries, $"Expected no journal entries in MongoDB for {pathfinderId}, but found some.");
    }
}
