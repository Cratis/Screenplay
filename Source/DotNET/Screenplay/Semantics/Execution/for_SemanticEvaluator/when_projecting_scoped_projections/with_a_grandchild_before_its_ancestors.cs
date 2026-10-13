// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

// Chronicle ResolveFutures loops until no progress: creating a child makes its pending grandchild resolvable in the same drain.
public class with_a_grandchild_before_its_ancestors : given.a_scoped_projection_plan
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
            count quantity
          every
            add subtotal by 1
          children parts identified by lineNumber
            from LineRemoved key lineNumber
              parent lineNumber
              count quantity
        """);

    void Because() => Project(
        Fact("LineRemoved", SecondOrder, ("orderId", Text(FirstOrder)), ("lineNumber", Number(3))),
        Fact("LineAdded", SecondOrder, ("orderId", Text(FirstOrder)), ("lineNumber", Number(3)), ("amount", Number(10))),
        Fact("OrderShipped", FirstOrder, ("carrier", Text("post"))));

    [Fact] void should_project() => _failure.ShouldBeNull();
    [Fact] void should_resolve_the_grandchild_in_the_same_drain() => ((SemanticArrayValue)Member(Line, "OrderLine", "parts")).Values.Length.ShouldEqual(1);
    [Fact] void should_apply_the_original_grandchild_occurrence() => SemanticValueRules.AreEqual(Member(Part, "OrderPart", "quantity"), Number(1)).ShouldBeTrue();
    [Fact] void should_apply_every_mappings_to_each_ancestor() => SemanticValueRules.AreEqual(Member(Line, "OrderLine", "subtotal"), Number(2)).ShouldBeTrue();
    [Fact] void should_apply_root_every_mappings_once_per_occurrence() => SemanticValueRules.AreEqual(Value(FirstOrder, "events"), Number(3)).ShouldBeTrue();

    SemanticValue Line => ((SemanticArrayValue)Value(FirstOrder, "lines")).Values.Single();
    SemanticValue Part => ((SemanticArrayValue)Member(Line, "OrderLine", "parts")).Values.Single();
}
