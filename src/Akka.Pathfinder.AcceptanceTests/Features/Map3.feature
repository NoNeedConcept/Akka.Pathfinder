Feature: Map3
 15*15*15 Points map... basic function test for pathfinder

    Scenario: Find long Path on Map 3
        Given Map is 3
        When You are on Point 1 and have the direction 0 want to find a Path to Point 3000 PathfinderId c0a24507-a220-4d8d-ae4f-71dd31174164 Seconds 10
        Then the path for PathfinderId c0a24507-a220-4d8d-ae4f-71dd31174164 should cost 3906

    Scenario: Find multiple long Paths on Map 3
        Given Map is 3
        When You are on Point 1 and have the direction 0 want to find a Path to Point 3000 PathfinderId e39cb9e2-7ab1-4e71-a603-2869e99939bb Seconds 10
        When You are on Point 1 and have the direction 0 want to find a Path to Point 2500 PathfinderId 28e45b4c-2b28-4f97-8e42-47c5ef8a94b0 Seconds 15
        Then the path for PathfinderId e39cb9e2-7ab1-4e71-a603-2869e99939bb should cost 3906
        Then the path for PathfinderId 28e45b4c-2b28-4f97-8e42-47c5ef8a94b0 should cost 2646