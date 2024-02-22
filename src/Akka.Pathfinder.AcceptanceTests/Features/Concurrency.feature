Feature: Concurrency
    @Acceptance
    Scenario: Multiple requests on one stream should be fast
        Given Map is 2
        When I send 2 requests simultaneously on one stream with timeout 10 seconds
