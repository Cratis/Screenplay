// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

// Chronicle ResolveFutures replays the original child event and context after the root's from establishes its parent.
public class a_child_event_before_its_parent : given.a_scoped_projection_plan
{
    void Establish() => Plan(
        """
        from OrderShipped
          label = carrier
        every
          count events
        children lines identified by lineNumber
          from LineAdded key lineNumber
            parent orderId
            orderId = $eventContext.eventSourceId
            subtotal = amount
            count quantity
        """);

    void Because() => Project(
        Fact("LineAdded", SecondOrder, ("orderId", Text(FirstOrder)), ("lineNumber", Number(1)), ("amount", Number(10))),
        Fact("OrderShipped", SecondOrder, ("carrier", Text("other"))),
        Fact("OrderShipped", FirstOrder, ("carrier", Text("post"))));

    [Fact] void should_project() => _failure.ShouldBeNull();
    [Fact] void should_apply_the_child_to_its_original_parent() => ((SemanticArrayValue)Value(FirstOrder, "lines")).Values.Length.ShouldEqual(1);
    [Fact] void should_not_use_the_later_events_parent() => ((SemanticArrayValue)Value(SecondOrder, "lines")).Values.ShouldBeEmpty();
    [Fact] void should_keep_the_original_event_context() => SemanticValueRules.AreEqual(Member(Line, "OrderLine", "orderId"), Text(SecondOrder)).ShouldBeTrue();
    [Fact] void should_keep_the_original_event_values() => SemanticValueRules.AreEqual(Member(Line, "OrderLine", "subtotal"), Number(10)).ShouldBeTrue();
    [Fact] void should_apply_ancestor_every_mappings_once() => SemanticValueRules.AreEqual(Value(FirstOrder, "events"), Number(2)).ShouldBeTrue();

    SemanticValue Line => ((SemanticArrayValue)Value(FirstOrder, "lines")).Values.Single();
}
