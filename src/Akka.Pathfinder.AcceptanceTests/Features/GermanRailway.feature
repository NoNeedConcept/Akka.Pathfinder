@Acceptance
Feature: GermanRailway
    Scenario: Find Path on German Railway Map 7 Low Detail From Hamburg to Hanover
        Given Map is 7
        When You are on Point 2720 and have the direction Top want to find a Path to Point 3118 PathfinderId 12345678-1234-1234-1234-123456789012 Seconds 20
        Then the path for PathfinderId 12345678-1234-1234-1234-123456789012 should cost 96

    Scenario: Find Path on German Railway Map 8 High Detail From Hamburg to Hanover
        Given Map is 8
        When You are on Point 6180 and have the direction Top want to find a Path to Point 7002 PathfinderId 22345678-1234-1234-1234-123456789012 Seconds 20
        Then the path for PathfinderId 22345678-1234-1234-1234-123456789012 should cost 152