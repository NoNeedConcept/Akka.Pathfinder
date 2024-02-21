Feature: Map5
 35*40*40 Points map... basic function test for pathfinder

    Scenario: Find long Path on Map 5
        Given Map is 5
        When You are on Point 1 and have the direction 0 want to find a Path to Point 25000 PathfinderId d91fabeb-9d8b-4491-a9fe-4bc174d7795f Seconds 10
        Then the path for PathfinderId d91fabeb-9d8b-4491-a9fe-4bc174d7795f should cost 7560

    Scenario: Find multiple long Paths on Map 5
        Given Map is 5
        When You are on Point 1 and have the direction 0 want to find a Path to Point 35000 PathfinderId fe022be3-ca51-4d00-899d-e32da084899f Seconds 10
        When You are on Point 1 and have the direction 0 want to find a Path to Point 50000 PathfinderId eac8a4fe-4b83-4f51-9f20-c8c8aae96f1d Seconds 15
        Then the path for PathfinderId fe022be3-ca51-4d00-899d-e32da084899f should cost 12222
        Then the path for PathfinderId eac8a4fe-4b83-4f51-9f20-c8c8aae96f1d should cost 10332