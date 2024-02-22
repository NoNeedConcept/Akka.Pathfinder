@Acceptance
Feature: Map1
 9 Points map... basic function test for pathfinder

    Scenario: Find long Path on Map 
        Given Map is 1
        When You are on Point 1 and have the direction 0 want to find a Path to Point 2 PathfinderId ac1c44ff-06a5-45dc-a73c-33bf05ca044f Seconds 5
        Then the path for PathfinderId ac1c44ff-06a5-45dc-a73c-33bf05ca044f should cost 0

    Scenario: Find simple Path on Map
        Given Map is 1
        When You are on Point 2 and have the direction 0 want to find a Path to Point 1 PathfinderId d956a05d-2c40-467e-8ac1-f9b63012e0dc Seconds 5
        Then the path for PathfinderId d956a05d-2c40-467e-8ac1-f9b63012e0dc should cost 0

    Scenario: Find simple Path and long Path on Map
        Given Map is 1
        When You are on Point 1 and have the direction 0 want to find a Path to Point 2 PathfinderId 1dff208f-7090-4c5f-bb61-b6bbc4e09a5c Seconds 5
        Then the path for PathfinderId 1dff208f-7090-4c5f-bb61-b6bbc4e09a5c should cost 0
        When You are on Point 2 and have the direction 0 want to find a Path to Point 1 PathfinderId bf71b1b2-766f-4f25-8d3c-7ec9536fe9c0 Seconds 5
        Then the path for PathfinderId bf71b1b2-766f-4f25-8d3c-7ec9536fe9c0 should cost 0