// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

public class without_the_deferred_childs_parent : given.a_scoped_projection_plan
{
    void Establish() => Plan("from OrderShipped\n  label = carrier\nchildren lines identified by lineNumber\n  from LineAdded key lineNumber\n    parent orderId\n    count quantity");

    void Because() => Project(
        Fact("LineAdded", SecondOrder, ("orderId", Text(FirstOrder)), ("lineNumber", Number(1)), ("amount", Number(10))),
        Fact("OrderShipped", SecondOrder, ("carrier", Text("other"))));

    [Fact] void should_run_without_a_projection_failure() => _failure.ShouldBeNull();
    [Fact] void should_not_create_the_missing_parent() => Instance(FirstOrder).ShouldBeNull();
    [Fact] void should_not_make_the_pending_child_visible() => ((SemanticArrayValue)Value(SecondOrder, "lines")).Values.ShouldBeEmpty();
}
