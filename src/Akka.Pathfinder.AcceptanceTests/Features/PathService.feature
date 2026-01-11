@Acceptance
Feature: PathService Additional gRPC Operations

    Scenario: Get an existing path
        Given Map is 1
        When You are on Point 1 and have the direction 0 want to find a Path to Point 2 PathfinderId ac1c44ff-06a5-45dc-a73c-33bf05ca044f Seconds 5
        Then the path for PathfinderId ac1c44ff-06a5-45dc-a73c-33bf05ca044f should cost 0
        When Get path with id from PathfinderId ac1c44ff-06a5-45dc-a73c-33bf05ca044f
        Then the retrieved path should contain valid points

    Scenario: Delete pathfinder
        Given Map is 1
        When You are on Point 1 and have the direction 0 want to find a Path to Point 2 PathfinderId d956a05d-2c40-467e-8ac1-f9b63012e0dc Seconds 5
        Then the path for PathfinderId d956a05d-2c40-467e-8ac1-f9b63012e0dc should cost 0
        When Delete pathfinder d956a05d-2c40-467e-8ac1-f9b63012e0dc
        Then the pathfinder deletion should be successful

    Scenario: Get path for non-existent path ID
        Given Map is 1
        When Get path with non-existent path id "00000000-0000-0000-0000-000000000000"
        Then the path retrieval should fail

    Scenario: Multiple pathfinders on same map
        Given Map is 1
        When You are on Point 1 and have the direction 0 want to find a Path to Point 2 PathfinderId 1dff208f-7090-4c5f-bb61-b6bbc4e09a5c Seconds 5
        And You are on Point 2 and have the direction 0 want to find a Path to Point 1 PathfinderId bf71b1b2-766f-4f25-8d3c-7ec9536fe9c0 Seconds 5
        Then the path for PathfinderId 1dff208f-7090-4c5f-bb61-b6bbc4e09a5c should cost 0
        And the path for PathfinderId bf71b1b2-766f-4f25-8d3c-7ec9536fe9c0 should cost 0
        When Get path with id from PathfinderId 1dff208f-7090-4c5f-bb61-b6bbc4e09a5c
        And Get path with id from PathfinderId bf71b1b2-766f-4f25-8d3c-7ec9536fe9c0
        Then both paths should be retrievable