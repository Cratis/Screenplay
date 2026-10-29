// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

public class without_a_required_occurrence : given.a_scoped_projection_plan
{
    [Fact]
    void should_not_defer_a_child_that_needs_missing_time()
    {
        Plan("from OrderShipped\n  label = carrier\nchildren lines identified by lineNumber\n  from LineAdded key lineNumber\n    parent orderId\n    subtotal = amount\n    lastSeen = $eventContext.occurred");
        _compilation.Issues.ShouldBeEmpty();
        var child = Fact("LineAdded", SecondOrder, ("orderId", Text(FirstOrder)), ("lineNumber", Number(1)), ("amount", Number(10)));
        var prior = new SemanticEvaluator.ProjectionState([], []);
        SemanticEvaluator.TryProject(_compilation.Plan!, [], prior, [child], out var after, out _failure).ShouldBeFalse();
        _failure.ShouldContain("occurrence time");
        after.ShouldBeSame(prior);
        after.PendingChildren.ShouldBeEmpty();
    }

    [Fact]
    void should_not_defer_a_child_removal_whose_ancestor_needs_missing_time()
    {
        Plan("from OrderShipped\nevery include children\n  lastSeen = $eventContext.occurred\nchildren lines identified by lineNumber\n  from LineAdded key lineNumber\n    parent orderId\n    count quantity\n  remove with LineRemoved key lineNumber\n    parent orderId");
        _compilation.Issues.ShouldBeEmpty();
        var removal = Fact("LineRemoved", SecondOrder, ("orderId", Text(FirstOrder)), ("lineNumber", Number(1)));
        var prior = new SemanticEvaluator.ProjectionState([], []);
        SemanticEvaluator.TryProject(_compilation.Plan!, [], prior, [removal], out var after, out _failure).ShouldBeFalse();
        _failure.ShouldContain("occurrence time");
        after.ShouldBeSame(prior);
        after.PendingChildren.ShouldBeEmpty();
    }

    [Fact]
    void should_remove_without_time_when_the_ancestor_handles_the_shared_removal_itself()
    {
        // The root removes itself for LineRemoved, so replaying the deferred child removal never applies the root's every mapping.
        Plan("from OrderShipped\nevery include children\n  lastSeen = $eventContext.occurred\nremove with LineRemoved key orderId\nchildren lines identified by lineNumber\n  from LineAdded key lineNumber\n    parent orderId\n    count quantity\n  remove with LineRemoved key lineNumber\n    parent orderId");
        _compilation.Issues.ShouldBeEmpty();
        var parent = Fact("OrderShipped", FirstOrder) with { Occurred = new DateTimeOffset(2026, 10, 2, 9, 0, 0, TimeSpan.Zero) };
        SemanticEvaluator.TryProject(_compilation.Plan!, [], new SemanticEvaluator.ProjectionState([], []), [parent], out var prior, out _failure).ShouldBeTrue();
        prior.ReadModels.Length.ShouldEqual(1);
        var removal = Fact("LineRemoved", SecondOrder, ("orderId", Text(FirstOrder)), ("lineNumber", Number(1)));
        SemanticEvaluator.TryProject(_compilation.Plan!, [parent], prior, [removal], out var after, out _failure).ShouldBeTrue();
        _failure.ShouldBeNull();
        after.ReadModels.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_publish_no_partial_state_when_a_later_mapping_needs_missing_time(bool deferred)
    {
        Plan("from OrderShipped\n  label = carrier\nchildren lines identified by lineNumber\n  from LineAdded key lineNumber\n    parent orderId\n    subtotal = amount\n    lastSeen = $eventContext.occurred");
        _compilation.Issues.ShouldBeEmpty();
        var child = Fact("LineAdded", SecondOrder, ("orderId", Text(FirstOrder)), ("lineNumber", Number(1)), ("amount", Number(10)));
        var timedChild = child with { Occurred = new DateTimeOffset(2026, 10, 2, 9, 0, 0, TimeSpan.Zero) };
        var orphan = Fact("LineAdded", SecondOrder, ("orderId", Text(SecondOrder)), ("lineNumber", Number(2)), ("amount", Number(5)));
        var parent = Fact("OrderShipped", FirstOrder, ("carrier", Text("before")));
        SemanticEvaluator.TryProject(_compilation.Plan!, [], new SemanticEvaluator.ProjectionState([], []), deferred ? [timedChild] : [parent], out var prior, out _failure).ShouldBeTrue();

        // Deferred: a pending child exists; a second child that would defer without its occurrence fails and leaves it pending.
        SemanticEvaluator.TryProject(_compilation.Plan!, deferred ? [timedChild] : [parent], prior, deferred ? [orphan] : [parent with { Values = [new(EventProperty("OrderShipped", "carrier"), Text("after"))] }, child], out var after, out _failure).ShouldBeFalse();
        _failure.ShouldContain("occurrence time");
        after.ShouldBeSame(prior);
        after.ReadModels.ShouldEqual(prior.ReadModels);
        after.PendingChildren.ShouldEqual(prior.PendingChildren);
        if (deferred)
        {
            after.ReadModels.ShouldBeEmpty();
            after.PendingChildren.Length.ShouldEqual(1);
        }
    }

    [Fact]
    void should_fail_even_when_the_target_is_optional()
    {
        Plan("from OrderShipped\n  label = carrier\n  lastSeen = $eventContext.occurred");
        _compilation.Issues.ShouldBeEmpty();
        SemanticEvaluator.TryProject(_compilation.Plan!, [], [], [Fact("OrderShipped", FirstOrder, ("carrier", Text("post")))], out var instances, out _failure).ShouldBeFalse();
        _failure.ShouldContain("occurrence time");
        instances.ShouldBeEmpty();
    }
}
