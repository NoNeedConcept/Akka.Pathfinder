Feature: Map4
 25*25*25 Points map... basic function test for pathfinder

    Scenario: Find long Path on Map 4
        Given Map is 4
        When You are on Point 1 and have the direction 0 want to find a Path to Point 3000 PathfinderId d91fabeb-9d8b-4491-a9fe-4bc174d7795f Seconds 5
        Then the path for PathfinderId d91fabeb-9d8b-4491-a9fe-4bc174d7795f should cost 5922

    Scenario: Find multiple long Paths on Map 4
        Given Map is 4
        When You are on Point 1 and have the direction 0 want to find a Path to Point 3000 PathfinderId 1fc137d8-9899-4fc2-8aa1-82a10fc1d504 Seconds 3
        When You are on Point 1 and have the direction 0 want to find a Path to Point 15000 PathfinderId fc0cd339-7d4e-4681-89a1-356461742def Seconds 8
        Then the path for PathfinderId 1fc137d8-9899-4fc2-8aa1-82a10fc1d504 should cost 5922
        Then the path for PathfinderId fc0cd339-7d4e-4681-89a1-356461742def should cost 8946