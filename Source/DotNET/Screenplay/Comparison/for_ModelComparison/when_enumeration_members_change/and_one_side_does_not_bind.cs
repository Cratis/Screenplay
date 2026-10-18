// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_enumeration_members_change;

public class and_one_side_does_not_bind : given.an_enumeration
{
    void Establish() => ChangeSource(Model("paid\n  issued\n  draft") + "    slice\n");
    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithIdentities(_after));

    [Fact] void should_report_known_number_changes() => _result.Enumerations.Count.ShouldEqual(2);
    [Fact] void should_report_incomplete_source() => _result.Sections.SelectMany(section => section.Gaps).Any(gap => gap.Kind == ComparisonGapKind.IncompleteSource).ShouldBeTrue();
    [Fact] void should_not_claim_completeness() => _result.Complete.ShouldBeFalse();
    [Fact] void should_report_known_semantic_change() => _result.HasSemanticChange.ShouldEqual(true);
    [Fact] void should_keep_the_candidate_non_executable() => _result.AfterExecutable.ShouldBeFalse();
}
