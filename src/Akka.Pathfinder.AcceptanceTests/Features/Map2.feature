@Acceptance
Feature: Map2
 27 Points map... basic function test for pathfinder

    Scenario: Find long Path on Map 2
        Given Map is 2
        When You are on Point 1 and have the direction 0 want to find a Path to Point 27 PathfinderId 463f1763-2eab-42a7-9a14-09f4316ea1ba Seconds 5
        Then the path for PathfinderId 463f1763-2eab-42a7-9a14-09f4316ea1ba should cost 0