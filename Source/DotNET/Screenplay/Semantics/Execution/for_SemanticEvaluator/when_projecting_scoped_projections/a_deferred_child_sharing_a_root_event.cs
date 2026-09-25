// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

// ResolveFutures replays only the deferred child projection, not root handlers for the same event
// (ResolveFutures.cs:74-200).
public class a_deferred_child_sharing_a_root_event : given.a_scoped_projection_plan
{
    const string Body =
        "from LineAdded\n  count events\nfrom OrderShipped\n  label = carrier\nchildren lines identified by lineNumber\n  from LineAdded key lineNumber\n    parent orderId\n    count quantity";

    void Establish() => Plan(Body);

    void Because() => Project(
        Fact("LineAdded", SecondOrder, ("orderId", Text(FirstOrder)), ("lineNumber", Number(1)), ("amount", Number(1))),
        Fact("LineAdded", SecondOrder, ("orderId", Text(FirstOrder)), ("lineNumber", Number(1)), ("amount", Number(1))),
        Fact("OrderShipped", FirstOrder, ("carrier", Text("post"))));

    [Fact] void should_project_deferred_children() => ((SemanticArrayValue)Value(FirstOrder, "lines")).Values.Length.ShouldEqual(1);
    [Fact] void should_not_reapply_the_independent_root_transition_when_retrying_children() =>
        SemanticValueRules.AreEqual(Value(SecondOrder, "events"), Number(2)).ShouldBeTrue();
}
