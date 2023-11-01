Feature: Map0
 2 Points map... basic function test for pathfinder

    Scenario: Find Path on Simple Map
        Given Map is 0
        When You are on Point 1 and have the direction 16 want to find a Path to Point 2
        Then the path should cost 84

    Scenario: Unreachable Pathfinder request on Simple Map
        Given Map is 0
        When You are on Point 2 and have the direction 16 want to find a Path to Point 1
        Then the path should not be found