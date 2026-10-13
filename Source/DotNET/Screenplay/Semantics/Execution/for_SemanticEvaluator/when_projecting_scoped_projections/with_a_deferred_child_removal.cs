// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

// Chronicle ChildEveryEventMappings.Subscribe includes child-only removals; ResolveFutures.ProvideToAncestors supplies deferred ones.
public class with_a_deferred_child_removal : given.a_scoped_projection_plan
{
    ImmutableArray<SemanticReadModelInstance> _ordered;
    string? _orderedFailure;

    void Establish() => Plan(
        """
        from OrderShipped
          label = carrier
        every
          count events
        children lines identified by lineNumber
          from LineAdded key lineNumber
            parent orderId
            count quantity
          remove with LineRemoved key lineNumber
            parent orderId
        """);

    void Because()
    {
        var root = Fact("OrderShipped", FirstOrder, ("carrier", Text("post")));
        var child = Fact("LineAdded", SecondOrder, ("orderId", Text(FirstOrder)), ("lineNumber", Number(1)), ("amount", Number(10)));
        var removal = Fact("LineRemoved", SecondOrder, ("orderId", Text(FirstOrder)), ("lineNumber", Number(1)));
        SemanticEvaluator.Establish(_compilation.Plan!, [], [root, child, removal], out _ordered, out _orderedFailure);
        Project(child, removal, root);
    }

    [Fact] void should_project_the_direct_removal() => _orderedFailure.ShouldBeNull();
    [Fact] void should_project_the_deferred_removal() => _failure.ShouldBeNull();
    [Fact] void should_remove_the_child_after_resolving_its_creation() => ((SemanticArrayValue)Value(FirstOrder, "lines")).Values.ShouldBeEmpty();
    [Fact] void should_apply_ancestor_every_mappings_for_the_direct_removal() => SemanticValueRules.AreEqual(_ordered.Single().Values.Single(value => value.TargetProperty == ReadModelProperty("OrderView", "events")).Value, Number(3)).ShouldBeTrue();
    [Fact] void should_apply_ancestor_every_mappings_for_the_deferred_removal() => SemanticValueRules.AreEqual(Value(FirstOrder, "events"), Number(3)).ShouldBeTrue();
}
