// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

public class an_occurred_child_mapping : given.a_scoped_projection_plan
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_keep_the_original_child_occurrence_in_the_child_and_ancestor(bool deferred)
    {
        Plan("from OrderShipped\nevery include children\n  lastSeen = $eventContext.occurred\nchildren lines identified by lineNumber\n  from LineAdded key lineNumber\n    parent orderId\n    count quantity\n    subtotal = amount\n    lastSeen = $eventContext.occurred");
        _compilation.Issues.ShouldBeEmpty();
        var child = Fact("LineAdded", SecondOrder, ("orderId", Text(FirstOrder)), ("lineNumber", Number(1)), ("amount", Number(10))) with
        {
            Occurred = new DateTimeOffset(2026, 10, 2, 9, 0, 0, TimeSpan.Zero).AddTicks(1234567)
        };
        var parent = Fact("OrderShipped", FirstOrder) with { Occurred = new DateTimeOffset(2026, 10, 3, 10, 0, 0, TimeSpan.Zero) };
        Project(deferred ? [child, parent] : [parent, child]);
        _failure.ShouldBeNull();
        if (deferred)
        {
            // Before the deferred child replays, the parent carries its own occurrence.
            SemanticEvaluator.Establish(_compilation.Plan!, [], [parent], out var parentOnly, out var parentFailure);
            parentFailure.ShouldBeNull();
            SemanticValueRules.AreEqual(
                parentOnly.Single().Values.Single(_ => _.TargetProperty == ReadModelProperty("OrderView", "lastSeen")).Value,
                Text("2026-10-03T10:00:00.0000000Z")).ShouldBeTrue();
        }

        var line = ((SemanticArrayValue)Value(FirstOrder, "lines")).Values.Single();
        SemanticValueRules.AreEqual(Member(line, "OrderLine", "lastSeen"), Text("2026-10-02T09:00:00.1234567Z")).ShouldBeTrue();
        SemanticValueRules.AreEqual(Value(FirstOrder, "lastSeen"), Text("2026-10-02T09:00:00.1234567Z")).ShouldBeTrue();
    }
}
