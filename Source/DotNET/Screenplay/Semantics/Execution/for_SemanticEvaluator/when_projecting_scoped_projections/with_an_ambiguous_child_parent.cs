// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

public class with_an_ambiguous_child_parent : given.a_scoped_projection_plan
{
    void Establish() => Plan(
        """
        from OrderShipped
          label = carrier
        children lines identified by lineNumber
          from LineAdded key lineNumber
            parent orderId
            count quantity
          children parts identified by lineNumber
            from LineRemoved key lineNumber
              parent lineNumber
              count quantity
        """);

    void Because() => Project(
        Fact("OrderShipped", FirstOrder, ("carrier", Text("first"))),
        Fact("OrderShipped", SecondOrder, ("carrier", Text("second"))),
        Fact("LineAdded", FirstOrder, ("orderId", Text(FirstOrder)), ("lineNumber", Number(1)), ("amount", Number(10))),
        Fact("LineAdded", SecondOrder, ("orderId", Text(SecondOrder)), ("lineNumber", Number(1)), ("amount", Number(20))),
        Fact("LineRemoved", SecondOrder, ("orderId", Text(FirstOrder)), ("lineNumber", Number(1))));

    [Fact] void should_keep_failing_closed() => _failure.ShouldEqual("Projection 'Orders' parent identity is ambiguous for a child event.");
    [Fact] void should_not_commit_partial_state() => _instances.ShouldBeEmpty();
}
