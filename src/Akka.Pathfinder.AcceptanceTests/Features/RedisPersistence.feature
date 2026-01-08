@Acceptance
@RedisActive
Feature: RedisPersistence

  Scenario: Verify that Akka Persistence uses Redis
    Given Map is 0
    When You are on Point 1 and have the direction Front want to find a Path to Point 2 PathfinderId BB0FC120-48B7-41B8-AC9F-719140383328 Seconds 10
    Then the path for PathfinderId BB0FC120-48B7-41B8-AC9F-719140383328 should cost 0
    And Redis should contain journal entries for PathfinderId BB0FC120-48B7-41B8-AC9F-719140383328
    And MongoDB should not contain journal entries for PathfinderId BB0FC120-48B7-41B8-AC9F-719140383328
