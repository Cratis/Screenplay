// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

// Whatever the reference evaluator cannot execute blocks the plan instead of binding and being ignored:
// - a projection-level 'remove via join', which Chronicle's engine wires as a child removal (ProjectionFactory.cs:297-308);
// - a join inside a nested object, which Chronicle's engine does not wire (ProjectionFactory.cs:415-470);
// - event-context values, which ESM v1 facts carry no occurrence context for.
// A fact without an event source fails when a transition keys on it.
public class unsupported_combinations : given.a_scoped_projection_plan
{
    SemanticExecutionPlanCompilation _rootJoinRemoval;
    SemanticExecutionPlanCompilation _joinInNested;
    SemanticExecutionPlanCompilation _eventContext;

    void Because()
    {
        Plan("from OrderShipped\nremove via join on CustomerClosed");
        _rootJoinRemoval = _compilation;
        Plan("nested shipping\n  join customer on carrier\n    with CustomerRegistered\n      note = customerName");
        _joinInNested = _compilation;
        Plan("from OrderShipped\n  lastSeen = $eventContext.occurred");
        _eventContext = _compilation;
        Plan("from OrderShipped\n  label = carrier");
        Project(Fact("OrderShipped", null, ("carrier", Text("post"))));
    }

    [Fact] void should_block_a_projection_level_join_removal() => _rootJoinRemoval.Issues.Single().Kind.ShouldEqual(SemanticPlanIssueKind.UnsupportedProjectionBlock);
    [Fact] void should_block_a_join_inside_a_nested_object() => _joinInNested.Issues.Single().Kind.ShouldEqual(SemanticPlanIssueKind.UnsupportedProjectionBlock);
    [Fact] void should_block_event_context_values() => _eventContext.Issues.Single().Kind.ShouldEqual(SemanticPlanIssueKind.UnsupportedEventContext);
    [Fact] void should_fail_a_fact_without_an_event_source() => _failure.ShouldContain("event source identity");
}
