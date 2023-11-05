Feature: Map1
 9 Points map... basic function test for pathfinder

    Scenario: Find long Path on Map 
        Given Map is 1
        When You are on Point 1 and have the direction 0 want to find a Path to Point 2 PathfinderId 463f1763-2eab-42a7-9a14-09f4316ea1ba Seconds 1
        Then the path for PathfinderId 463f1763-2eab-42a7-9a14-09f4316ea1ba should cost 420

    Scenario: Find simple Path on Map
        Given Map is 1
        When You are on Point 2 and have the direction 0 want to find a Path to Point 1 PathfinderId d956a05d-2c40-467e-8ac1-f9b63012e0dc Seconds 1
        Then the path for PathfinderId d956a05d-2c40-467e-8ac1-f9b63012e0dc should cost 84

    Scenario: Find simple Path and long Path on Map
        Given Map is 1
        When You are on Point 1 and have the direction 0 want to find a Path to Point 2 PathfinderId 1dff208f-7090-4c5f-bb61-b6bbc4e09a5c Seconds 2
        Then the path for PathfinderId 1dff208f-7090-4c5f-bb61-b6bbc4e09a5c should cost 420