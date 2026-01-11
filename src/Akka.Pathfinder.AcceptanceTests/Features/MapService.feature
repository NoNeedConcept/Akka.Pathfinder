@Acceptance
Feature: MapService gRPC Operations

    Scenario: Create a map successfully
        When Create a map with id "00000000-0000-0000-0000-000000000001" with 10 points from Map 1
        Then the map creation should be successful for map "00000000-0000-0000-0000-000000000001"
        And the map should contain 10 points

    Scenario: Load an existing map
        When Create a map with id "00000000-0000-0000-0000-000000000002" with 10 points from Map 1
        And Load map "00000000-0000-0000-0000-000000000002"
        Then the map load should be successful

    Scenario: Get state of an existing map
        When Create a map with id "00000000-0000-0000-0000-000000000003" with 10 points from Map 1
        And Get the state of map "00000000-0000-0000-0000-000000000003"
        Then the map state should indicate ready status

    Scenario: Delete an existing map
        When Create a map with id "00000000-0000-0000-0000-000000000004" with 10 points from Map 1
        And Delete map "00000000-0000-0000-0000-000000000004"
        Then the map deletion should be successful

    Scenario: Get state of non-existent map
        When Get the state of map "00000000-0000-0000-0000-000000000099"
        Then the map state retrieval should fail

    Scenario: Load non-existent map
        When Load map "00000000-0000-0000-0000-000000000099"
        Then the map load should fail

    Scenario: Delete non-existent map
        When Delete map "00000000-0000-0000-0000-000000000099"
        Then the map deletion should fail

    Scenario: Multiple maps in sequence
        When Create a map with id "00000000-0000-0000-0000-000000000010" with 10 points from Map 1
        And Create a map with id "00000000-0000-0000-0000-000000000011" with 10 points from Map 1
        Then the map creation should be successful for map "00000000-0000-0000-0000-000000000010"
        When Get the state of map "00000000-0000-0000-0000-000000000010"
        And Get the state of map "00000000-0000-0000-0000-000000000011"
        Then the map state should indicate ready status

    Scenario: Create, Load, and Delete map in sequence
        When Create a map with id "00000000-0000-0000-0000-000000000012" with 10 points from Map 1
        And Load map "00000000-0000-0000-0000-000000000012"
        And Get the state of map "00000000-0000-0000-0000-000000000012"
        Then the map state should indicate ready status
        When Delete map "00000000-0000-0000-0000-000000000012"
        Then the map deletion should be successful

