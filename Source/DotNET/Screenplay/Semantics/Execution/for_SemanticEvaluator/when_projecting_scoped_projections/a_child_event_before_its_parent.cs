// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

// Chronicle stores parentless child events as futures and retries them in insertion order after each event
// (KeyResolvers.cs:768-784; ProjectionPipelineManager.cs:76-86; ResolveFutures.cs:33-200; ProjectionFutures.cs:27).
public class a_child_event_before_its_parent : given.a_scoped_projection_plan
{
    const string Body =
        "from OrderShipped\n  label = carrier\nchildren lines identified by lineNumber\n  from LineAdded key lineNumber\n    parent orderId\n    subtotal = amount\n    count quantity";

    string? _missingFailure;
    ImmutableArray<SemanticReadModelInstance> _missing;
    SemanticExecutionResult _when;

    void Establish() => Plan(Body);

    void Because()
    {
        var first = Fact("LineAdded", "x", ("orderId", Text(FirstOrder)), ("lineNumber", Number(1)), ("amount", Number(10)));
        var second = Fact("LineAdded", "x", ("orderId", Text(FirstOrder)), ("lineNumber", Number(1)), ("amount", Number(5)));
        SemanticEvaluator.Establish(_compilation.Plan!, [], [first], out _missing, out _missingFailure);
        Project(first, second, Fact("OrderShipped", FirstOrder, ("carrier", Text("post"))));
        _when = SemanticEvaluator.Append(
            _compilation.Plan!,
            SemanticWorld.Create([first], _missing),
            Fact("OrderShipped", FirstOrder, ("carrier", Text("post"))),
            [],
            null);
    }

    [Fact] void should_not_fail_when_the_parent_never_appears() => _missingFailure.ShouldBeNull();
    [Fact] void should_leave_the_world_unchanged_without_a_parent() => _missing.ShouldBeEmpty();
    [Fact] void should_resolve_when_the_parent_appears() => _failure.ShouldBeNull();
    [Fact] void should_carry_pending_children_from_given_into_when() =>
        ((SemanticArrayValue)_when.World.ReadModels.Single().Values.Single(_ => _.TargetProperty == ReadModelProperty("OrderView", "lines")).Value).Values.Length.ShouldEqual(1);
    [Fact] void should_replay_several_pending_children_in_order() =>
        SemanticValueRules.AreEqual(Member(((SemanticArrayValue)Value(FirstOrder, "lines")).Values.Single(), "OrderLine", "subtotal"), Number(5)).ShouldBeTrue();
}
