// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

public class an_occurred_value_outside_property_mappings : given.a_scoped_projection_plan
{
    [Theory]
    [InlineData("simple")]
    [InlineData("composite")]
    [InlineData("parts")]
    [InlineData("parent")]
    [InlineData("removal")]
    [InlineData("removal-parent")]
    [InlineData("join-removal")]
    [InlineData("join-key")]
    [InlineData("join-mapping")]
    [InlineData("every-with-join")]
    [InlineData("arithmetic")]
    void should_refuse_occurred_in_excluded_positions_at_every_depth(string position)
    {
        Plan("from OrderShipped\n  lastSeen = $eventContext.occurred");
        var transition = Scope.From.Single();
        var occurred = transition.Mappings.Single().Source!;
        var key = new SemanticProjectionValueKey(occurred);
        var part = new SemanticProjectionKeyPart(ReadModelProperty("OrderView", "lastSeen"), occurred);
        var joined = new SemanticProjectionJoin(transition.EventContract, part.Property, []);
        var scope = position switch
        {
            "simple" => Scope with { From = [transition with { Key = key, Mappings = [] }] },
            "composite" => Scope with { From = [transition with { Key = new SemanticProjectionCompositeKey(part.Property, [part]), Mappings = [] }] },
            "parts" => Scope with { From = [transition with { Key = new SemanticProjectionPartsKey([part]), Mappings = [] }] },
            "parent" => Scope with { From = [transition with { ParentKey = key, Mappings = [] }] },
            "removal" => SemanticProjectionScope.Empty with { Removals = [new(transition.EventContract, key, null)] },
            "removal-parent" => SemanticProjectionScope.Empty with { Removals = [new(transition.EventContract, transition.Key, key)] },
            "join-removal" => SemanticProjectionScope.Empty with { JoinRemovals = [new(transition.EventContract, key)] },
            "join-key" => SemanticProjectionScope.Empty with { Joins = [joined with { Key = key }] },
            "join-mapping" => SemanticProjectionScope.Empty with { Joins = [joined with { Mappings = transition.Mappings }] },
            "every-with-join" => SemanticProjectionScope.Empty with { Joins = [joined], Every = new(true, false, transition.Mappings) },
            _ => Scope with { From = [transition with { Mappings = [transition.Mappings.Single() with { Operation = SemanticProjectionOperation.Add }] }] }
        };
        AssertRefused(scope);
        AssertRefused(SemanticProjectionScope.Empty with { Children = [new(part.Property, part.Property, scope)] });
        AssertRefused(SemanticProjectionScope.Empty with { Nested = [new(part.Property, scope)] });
    }

    [Fact]
    void should_refuse_occurred_properties_in_nested_objects()
    {
        Plan("nested shipping\n  from OrderShipped\n    lastSeen = $eventContext.occurred");
        _compilation.Issues.ShouldContain(issue => issue.Kind == SemanticPlanIssueKind.UnsupportedEventContext);
    }

    void AssertRefused(SemanticProjectionScope scope) =>
        SemanticScopedProjectionIssues.For(Projection with { Scope = scope }).ShouldContain(issue => issue.Kind == SemanticPlanIssueKind.UnsupportedEventContext);
}
