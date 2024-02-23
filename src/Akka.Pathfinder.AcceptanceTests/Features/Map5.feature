@Acceptance
Feature: Map5
 35*40*40 Points map... basic function test for pathfinder

    Scenario: Find long Path on Map 5
        Given Map is 5
        When You are on Point 1 and have the direction 0 want to find a Path to Point 20000 PathfinderId e39cb9e2-7ab1-4e71-a603-2869e99939bb Seconds 30
        Then the path for PathfinderId e39cb9e2-7ab1-4e71-a603-2869e99939bb should cost 0

    Scenario: Find multiple long Paths on Map 5
        Given Map is 5
        When You are on Point 1 and have the direction 0 want to find a Path to Point 35000 PathfinderId fe022be3-ca51-4d00-899d-e32da084899f Seconds 30
        And You are on Point 1 and have the direction 0 want to find a Path to Point 50000 PathfinderId eac8a4fe-4b83-4f51-9f20-c8c8aae96f1d Seconds 30
        Then the path for PathfinderId fe022be3-ca51-4d00-899d-e32da084899f should cost 0
        And the path for PathfinderId eac8a4fe-4b83-4f51-9f20-c8c8aae96f1d should cost 0