// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

public class unsupported_nested_combinations : given.a_nested_join_projection
{
    ImmutableArray<SemanticPlanIssue> _joinInChildren;
    ImmutableArray<SemanticPlanIssue> _childrenInNested;
    ImmutableArray<SemanticPlanIssue> _joinRemovalInNested;

    void Because()
    {
        Plan(ChildFrom + """

                join linked on reference
                  with Joined
                    value = value
            """);
        _joinInChildren = _compilation.Issues;
        Plan("""
            from Created
              label = reference
            nested details
              children children identified by id
                from Attached key itemId
                  parent rootId
                  id = itemId
            """);
        _childrenInNested = _compilation.Issues;
        Plan("""
            from Created
              label = reference
            nested details
              from Created
                reference = reference
              remove via join on Cleared
            """);
        _joinRemovalInNested = _compilation.Issues;
    }

    [Fact] void should_still_block_nested_joins_under_children() => _joinInChildren.Single().Kind.ShouldEqual(SemanticPlanIssueKind.UnsupportedProjectionBlock);
    [Fact] void should_explain_the_nested_join_refusal() => _joinInChildren.Single().Details.ShouldContain("A join inside a nested object under children is not wired by Chronicle's engine.");
    [Fact] void should_still_block_children_inside_nested_objects() => _childrenInNested.Single().Kind.ShouldEqual(SemanticPlanIssueKind.UnsupportedProjectionBlock);
    [Fact] void should_explain_the_nested_children_refusal() => _childrenInNested.Single().Details.ShouldContain("A children block or 'remove via join' inside a nested object is not wired by Chronicle's engine.");
    [Fact] void should_still_block_join_removals_inside_nested_objects() => _joinRemovalInNested.Single().Kind.ShouldEqual(SemanticPlanIssueKind.UnsupportedProjectionBlock);
    [Fact] void should_explain_the_nested_join_removal_refusal() => _joinRemovalInNested.Single().Details.ShouldContain("A children block or 'remove via join' inside a nested object is not wired by Chronicle's engine.");
}
