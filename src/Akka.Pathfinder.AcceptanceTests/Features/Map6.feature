Feature: Map6
 256*256*1 Points map... basic function test for pathfinder

    Scenario: Find long Path on Map 6
        Given Map is 6
        When You are on Point 1 and have the direction 0 want to find a Path to Point 25000 PathfinderId e39cb9e2-7ab1-4e71-a603-2869e99939bb Seconds 10
        Then the path for PathfinderId e39cb9e2-7ab1-4e71-a603-2869e99939bb should cost 34020

    Scenario: Find multiple long Paths on Map 6
        Given Map is 6
        When You are on Point 1 and have the direction 0 want to find a Path to Point 35000 PathfinderId fe022be3-ca51-4d00-899d-e32da084899f Seconds 10
        When You are on Point 1 and have the direction 0 want to find a Path to Point 50000 PathfinderId eac8a4fe-4b83-4f51-9f20-c8c8aae96f1d Seconds 15
        Then the path for PathfinderId fe022be3-ca51-4d00-899d-e32da084899f should cost 42462
        Then the path for PathfinderId eac8a4fe-4b83-4f51-9f20-c8c8aae96f1d should cost 42462