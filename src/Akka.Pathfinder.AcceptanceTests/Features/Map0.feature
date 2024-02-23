@Acceptance
Feature: Map0
 2 Points map... basic function test for pathfinder
    
    Scenario: Find Path on Simple Map
        Given Map is 0
        When You are on Point 1 and have the direction 0 want to find a Path to Point 2 PathfinderId 96db06d6-8569-4611-960c-3d67f74819b5 Seconds 1
        Then the path for PathfinderId 96db06d6-8569-4611-960c-3d67f74819b5 should cost 0

    Scenario: Unreachable Pathfinder request on Simple Map
        Given Map is 0
        When You are on Point 2 and have the direction 0 want to find a Path to Point 1 PathfinderId 09da8706-2221-417d-8499-28cbe88a39b5 Seconds 1 
        Then the path for PathfinderId 09da8706-2221-417d-8499-28cbe88a39b5 should not be found