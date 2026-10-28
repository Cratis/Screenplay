// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

// Admission refuses these plans; the executor must still refuse occurred outside property assignments if one reaches it.
public class an_occurred_value_bypassing_admission : given.a_scoped_projection_plan
{
    [Theory]
    [InlineData("key")]
    [InlineData("join-key")]
    [InlineData("join-mapping")]
    void should_refuse_to_resolve_occurred(string position)
    {
        Plan("from OrderShipped\n  label = carrier\n  lastSeen = $eventContext.occurred");
        _compilation.Issues.ShouldBeEmpty();
        var transition = Scope.From.Single();
        var occurredMapping = transition.Mappings.Single(_ => _.Source is SemanticProjectionEventContextValue);
        var label = transition.Mappings.Single(_ => _.Source is not SemanticProjectionEventContextValue);
        var occurredKey = new SemanticProjectionValueKey(occurredMapping.Source!);

        // The join matches the label, which the from sets to the event source identity, so the join applies to the same instance.
        var join = new SemanticProjectionJoin(transition.EventContract, ReadModelProperty("OrderView", "label"), []);
        var scope = position switch
        {
            "key" => Scope with { From = [transition with { Key = occurredKey, Mappings = [label] }] },
            "join-key" => Scope with { From = [transition with { Mappings = [label] }], Joins = [join with { Key = occurredKey }] },
            _ => Scope with { From = [transition with { Mappings = [label] }], Joins = [join with { Mappings = [occurredMapping] }] }
        };
        var fact = Fact("OrderShipped", FirstOrder, ("carrier", Text(FirstOrder))) with { Occurred = new DateTimeOffset(2026, 10, 2, 9, 0, 0, TimeSpan.Zero) };

        SemanticEvaluator.TryProject(WithScope(scope), [], [], [fact], out var instances, out _failure).ShouldBeFalse();
        _failure.ShouldContain("no occurrence context");
        instances.ShouldBeEmpty();
    }

    SemanticExecutionPlan WithScope(SemanticProjectionScope scope)
    {
        var plan = _compilation.Plan!;
        return new SemanticExecutionPlan(
            plan.Model,
            plan.Commands,
            plan.Events,
            plan.Projections.SetItem(Projection.Id, Projection with { Scope = scope }),
            plan.ReadModels,
            plan.Queries,
            plan.Specifications,
            plan.Constraints,
            plan.Reactions,
            plan.Captures,
            plan.UnadmittedArtifacts);
    }
}
