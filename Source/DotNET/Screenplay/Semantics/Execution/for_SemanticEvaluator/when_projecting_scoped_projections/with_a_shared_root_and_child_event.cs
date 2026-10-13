// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

// Chronicle FindParentEvent treats a shared event as the parent event and resolves the root's key, ignoring the child's parent expression.
public class with_a_shared_root_and_child_event : given.a_scoped_projection_plan
{
    void Establish() => Plan(
        """
        from LineAdded
          orderId = $eventContext.eventSourceId
          label = "root"
        every
          count events
        children lines identified by lineNumber
          from LineAdded key lineNumber
            parent orderId
            subtotal = amount
            count quantity
        """);

    void Because() => Project(
        Fact("LineAdded", FirstOrder, ("orderId", Text(SecondOrder)), ("lineNumber", Number(1)), ("amount", Number(10))),
        Fact("LineAdded", FirstOrder, ("orderId", Text(SecondOrder)), ("lineNumber", Number(2)), ("amount", Number(20))),
        Fact("LineAdded", FirstOrder, ("orderId", Text(SecondOrder)), ("lineNumber", Number(1)), ("amount", Number(30))));

    [Fact] void should_project() => _failure.ShouldBeNull();
    [Fact] void should_not_route_to_the_configured_parent_key() => Instance(SecondOrder).ShouldBeNull();
    [Fact] void should_keep_siblings_under_the_resolved_root() => Lines.Length.ShouldEqual(2);
    [Fact] void should_update_the_named_child_not_its_sibling() => SemanticValueRules.AreEqual(Member(Lines[0], "OrderLine", "subtotal"), Number(30)).ShouldBeTrue();
    [Fact] void should_not_apply_root_every_mappings_twice() => SemanticValueRules.AreEqual(Value(FirstOrder, "events"), Number(3)).ShouldBeTrue();

    SemanticValue[] Lines => [.. ((SemanticArrayValue)Value(FirstOrder, "lines")).Values];
}
