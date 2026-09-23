// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

// 'remove via join' removes every child with the identity across all parents (Chronicle ProjectionFactory.SetupRemovedWithJoin,
// ProjectionFactory.cs:297-308).
public class a_join_removal_inside_children : given.a_scoped_projection_plan
{
    const string Body =
        """
        from OrderShipped
          label = carrier
        children lines identified by lineNumber
          from LineAdded key lineNumber
            parent orderId
            count quantity
          remove via join on LineRemoved key lineNumber
        """;

    void Establish() => Plan(Body);

    void Because() => Project(
        Fact("OrderShipped", FirstOrder, ("carrier", Text("post"))),
        Fact("OrderShipped", SecondOrder, ("carrier", Text("air"))),
        Fact("LineAdded", "x", ("orderId", Text(FirstOrder)), ("lineNumber", Number(7)), ("amount", Number(1))),
        Fact("LineAdded", "x", ("orderId", Text(SecondOrder)), ("lineNumber", Number(7)), ("amount", Number(1))),
        Fact("LineAdded", "x", ("orderId", Text(SecondOrder)), ("lineNumber", Number(8)), ("amount", Number(1))),
        Fact("LineRemoved", "x", ("orderId", Text(FirstOrder)), ("lineNumber", Number(7))));

    [Fact] void should_project() => _failure.ShouldBeNull();
    [Fact] void should_remove_the_child_from_every_parent() =>
        (Lines(FirstOrder).Length + Lines(SecondOrder).Length).ShouldEqual(1);
    [Fact] void should_keep_other_children() => SemanticValueRules.AreEqual(Member(Lines(SecondOrder).Single(), "OrderLine", "lineNumber"), Number(8)).ShouldBeTrue();

    SemanticValue[] Lines(string order) => [.. ((SemanticArrayValue)Value(order, "lines")).Values];
}
