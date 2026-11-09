// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

// 'sort by' orders the collection when a child is added. An update to an existing child - even to its sort property - never
// moves it; the next added child sorts the whole collection again, stably (Chronicle#5758).
public class children_sorted_by_a_property : given.a_scoped_projection_plan
{
    const string Body =
        """
        from OrderShipped
          label = carrier
        children lines identified by lineNumber
          sort by subtotal descending
          from LineAdded key lineNumber
            parent orderId
            add subtotal by amount
            count quantity
        """;

    decimal[] _afterUpdate;

    void Establish() => Plan(Body);

    void Because()
    {
        Project(
            Fact("OrderShipped", FirstOrder, ("carrier", Text("post"))),
            Fact("LineAdded", "x", ("orderId", Text(FirstOrder)), ("lineNumber", Number(1)), ("amount", Number(5))),
            Fact("LineAdded", "x", ("orderId", Text(FirstOrder)), ("lineNumber", Number(2)), ("amount", Number(10))),
            Fact("LineAdded", "x", ("orderId", Text(FirstOrder)), ("lineNumber", Number(3)), ("amount", Number(1))),
            Fact("LineAdded", "x", ("orderId", Text(FirstOrder)), ("lineNumber", Number(3)), ("amount", Number(20))));
        _afterUpdate = LineNumbers;
        Project(
            Fact("OrderShipped", FirstOrder, ("carrier", Text("post"))),
            Fact("LineAdded", "x", ("orderId", Text(FirstOrder)), ("lineNumber", Number(1)), ("amount", Number(5))),
            Fact("LineAdded", "x", ("orderId", Text(FirstOrder)), ("lineNumber", Number(2)), ("amount", Number(10))),
            Fact("LineAdded", "x", ("orderId", Text(FirstOrder)), ("lineNumber", Number(3)), ("amount", Number(1))),
            Fact("LineAdded", "x", ("orderId", Text(FirstOrder)), ("lineNumber", Number(3)), ("amount", Number(20))),
            Fact("LineAdded", "x", ("orderId", Text(FirstOrder)), ("lineNumber", Number(4)), ("amount", Number(7))));
    }

    [Fact] void should_project() => _failure.ShouldBeNull();
    [Fact] void should_not_move_an_updated_child() => _afterUpdate.ShouldEqual([2m, 1m, 3m]);
    [Fact] void should_sort_the_collection_when_a_child_is_added() => LineNumbers.ShouldEqual([3m, 2m, 4m, 1m]);

    decimal[] LineNumbers => [.. ((SemanticArrayValue)Value(FirstOrder, "lines")).Values.Select(line => ((SemanticNumberValue)Member(line, "OrderLine", "lineNumber")).Value)];
}
