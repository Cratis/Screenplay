// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

public class with_two_deferred_children : given.a_scoped_projection_plan
{
    void Establish() => Plan("from OrderShipped\n  label = carrier\nchildren lines identified by lineNumber\n  from LineAdded key lineNumber\n    parent orderId\n    subtotal = amount\n    count quantity");

    void Because() => Project(
        Fact("LineAdded", SecondOrder, ("orderId", Text(FirstOrder)), ("lineNumber", Number(2)), ("amount", Number(10))),
        Fact("LineAdded", SecondOrder, ("orderId", Text(FirstOrder)), ("lineNumber", Number(1)), ("amount", Number(20))),
        Fact("LineAdded", SecondOrder, ("orderId", Text(FirstOrder)), ("lineNumber", Number(2)), ("amount", Number(30))),
        Fact("OrderShipped", FirstOrder, ("carrier", Text("post"))));

    [Fact] void should_project() => _failure.ShouldBeNull();
    [Fact] void should_add_children_in_arrival_order() => Lines.Select(line => Member(line, "OrderLine", "lineNumber")).ShouldContainOnly(Number(2), Number(1));
    [Fact] void should_apply_later_occurrences_last() => SemanticValueRules.AreEqual(Member(Lines[0], "OrderLine", "subtotal"), Number(30)).ShouldBeTrue();
    [Fact] void should_apply_each_occurrence_once() => SemanticValueRules.AreEqual(Member(Lines[0], "OrderLine", "quantity"), Number(2)).ShouldBeTrue();

    SemanticValue[] Lines => [.. ((SemanticArrayValue)Value(FirstOrder, "lines")).Values];
}
