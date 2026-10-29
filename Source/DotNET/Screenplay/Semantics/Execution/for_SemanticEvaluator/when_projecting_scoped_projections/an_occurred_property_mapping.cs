// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

public class an_occurred_property_mapping : given.a_scoped_projection_plan
{
    [Theory]
    [InlineData("from OrderShipped\n  lastSeen = $eventContext.occurred")]
    [InlineData("every\n  lastSeen = $eventContext.occurred\nfrom OrderShipped")]
    [InlineData("all\n  lastSeen = $eventContext.occurred\nfrom OrderPlaced")]
    void should_map_the_current_facts_exact_utc_occurrence(string body)
    {
        Plan(body);
        _compilation.Issues.ShouldBeEmpty();
        Project(
            Fact("OrderShipped", FirstOrder) with { Occurred = new DateTimeOffset(2026, 10, 2, 11, 0, 0, TimeSpan.FromHours(2)).AddTicks(1234567) },
            Fact("OrderShipped", FirstOrder) with { Occurred = new DateTimeOffset(2026, 10, 3, 12, 0, 0, TimeSpan.FromHours(2)).AddTicks(7654321) });
        _failure.ShouldBeNull();
        SemanticValueRules.AreEqual(Value(FirstOrder, "lastSeen"), Text("2026-10-03T10:00:00.7654321Z")).ShouldBeTrue();
    }
}
