// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Indexing.for_AuthoringIndex.given;

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_a_projection_publishes_a_public_event;

public class when_preserving_event_target_boundaries : given.two_models
{
    (string Name, ModelDifference Difference, bool Implicit, bool Unresolved)[] _cases = [];

    void Because() => _cases = [.. event_target_applications.Boundaries().Select(value =>
    {
        var workspace = Create(string.Join('\n', value.Sources.Values), application: "Contracts");
        return (value.Name, ModelComparison.Compare(ComparedModel.WithoutIdentities(workspace), ComparedModel.WithoutIdentities(workspace)), value.Implicit, value.Candidates.Length == 0);
    })];

    [Fact] void should_not_invent_changes_for_any_excluded_case() => _cases.Select(value => value.Difference.Declarations.Count + value.Difference.Members.Count + value.Difference.Events.Count + value.Difference.Specifications.Count + value.Difference.Dependants.Count).ShouldEqual(Enumerable.Repeat(0, 17));

    [Fact]
    void should_keep_unresolved_dependant_gaps_and_their_statements()
    {
        foreach (var value in _cases)
        {
            value.Difference.Sections.Single(section => section.Section == ComparisonSectionKind.Dependants).Gaps.Where(gap => gap.Kind == ComparisonGapKind.UnresolvedDependants).Select(gap => gap.Statement).ShouldEqual(value.Unresolved ? ["Dependency index contains unresolved, ambiguous or incomplete references; candidates are reported, not proven runtime dependants."] : []);
        }
    }

    [Fact]
    void should_keep_unsupported_implicit_views_and_ambiguous_event_groups_visible()
    {
        foreach (var value in _cases)
        {
            value.Difference.Sections.Single(section => section.Section == ComparisonSectionKind.Declarations).Gaps.Where(gap => gap.Kind == ComparisonGapKind.NotComparableIndexed).Select(gap => gap.Statement).ShouldEqual(value.Implicit || value.Name == "qualified target" || value.Name == "duplicate local events" ? ["1 indexed kind/address groups have no comparable authored members; ambiguous or unsupported groups were not discarded."] : []);
        }
    }
}
