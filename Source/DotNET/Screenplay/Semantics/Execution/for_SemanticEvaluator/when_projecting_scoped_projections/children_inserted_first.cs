// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

// 'insert first' puts a new child at the start of the collection; an update to an existing child never moves it (Chronicle#5758).
public class children_inserted_first : given.a_scoped_projection_plan
{
    const string Body =
        """
        from OrderShipped
          label = carrier
        children lines identified by lineNumber
          insert first
          from LineAdded key lineNumber
            parent orderId
            add subtotal by amount
            count quantity
        """;

    void Establish() => Plan(Body);

    void Because()
    {
        Project(
            Fact("OrderShipped", FirstOrder, ("carrier", Text("post"))),
            Fact("LineAdded", "x", ("orderId", Text(FirstOrder)), ("lineNumber", Number(1)), ("amount", Number(1))),
            Fact("LineAdded", "x", ("orderId", Text(FirstOrder)), ("lineNumber", Number(2)), ("amount", Number(2))),
            Fact("LineAdded", "x", ("orderId", Text(FirstOrder)), ("lineNumber", Number(3)), ("amount", Number(3))),
            Fact("LineAdded", "x", ("orderId", Text(FirstOrder)), ("lineNumber", Number(1)), ("amount", Number(4))));
    }

    [Fact] void should_project() => _failure.ShouldBeNull();
    [Fact] void should_put_the_newest_child_first() => LineNumbers.ShouldEqual([3m, 2m, 1m]);
    [Fact] void should_update_a_child_where_it_is() => SemanticValueRules.AreEqual(Member(Lines[2], "OrderLine", "subtotal"), Number(5)).ShouldBeTrue();

    SemanticValue[] Lines => [.. ((SemanticArrayValue)Value(FirstOrder, "lines")).Values];

    decimal[] LineNumbers => [.. Lines.Select(line => ((SemanticNumberValue)Member(line, "OrderLine", "lineNumber")).Value)];
}
