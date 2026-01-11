@Acceptance
Feature: PointService gRPC Operations

    Background:
        Given Map is 1

    Scenario: Occupy a point
        When Occupy point 1
        Then the point operation should succeed

    Scenario: Release an occupied point
        When Occupy point 2
        And Release point 2
        Then the point operation should succeed

    Scenario: Block a point
        When Block point 3
        Then the point operation should succeed

    Scenario: Unblock a blocked point
        When Block point 4
        And Unblock point 4
        Then the point operation should succeed

    Scenario: Increase point cost
        When Increase cost of point 5 by 10
        Then the point operation should succeed

    Scenario: Decrease point cost
        When Increase cost of point 6 by 20
        And Decrease cost of point 6 by 5
        Then the point operation should succeed

    Scenario: Increase direction cost
        When Increase direction cost of point 1 in direction 3 by 5
        Then the point operation should succeed

    Scenario: Decrease direction cost
        When Increase direction cost of point 2 in direction 2 by 15
        And Decrease direction cost of point 2 in direction 2 by 3
        Then the point operation should succeed

    Scenario: Update point direction configuration
        When Update direction config for point 3 with direction 4
        Then the point operation should succeed

    Scenario: Multiple point operations in sequence
        When Occupy point 1
        And Block point 2
        And Increase cost of point 3 by 10
        And Increase direction cost of point 4 in direction 4 by 5
        Then the point operation should succeed

    Scenario: Increase direction cost on multiple points
        When Increase direction cost of point 2 in direction 3 by 8
        And Increase direction cost of point 4 in direction 3 by 6
        Then the point operation should succeed

    Scenario: Decrease direction cost sequence
        When Increase direction cost of point 5 in direction 2 by 12
        And Decrease direction cost of point 5 in direction 2 by 4
        Then the point operation should succeed

    Scenario: Occupy with direction cost operations
        When Occupy point 1
        And Increase cost of point 1 by 5
        And Increase direction cost of point 1 in direction 3 by 7
        Then the point operation should succeed

    Scenario: Block with direction cost operations
        When Block point 6
        And Increase direction cost of point 6 in direction 2 by 10
        And Increase direction cost of point 6 in direction 4 by 5
        Then the point operation should succeed