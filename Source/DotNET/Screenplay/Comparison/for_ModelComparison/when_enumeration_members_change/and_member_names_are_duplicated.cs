// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_enumeration_members_change;

public class and_member_names_are_duplicated : given.an_enumeration
{
    void Establish() => Models(Positional, "draft\n  draft\n  paid");
    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithIdentities(_after));

    [Fact] void should_not_invent_numeric_identity_changes() => _result.Enumerations.ShouldBeEmpty();
    [Fact] void should_report_the_enum_gap() => _result.Sections.SelectMany(section => section.Gaps).Any(gap => gap.Kind == ComparisonGapKind.EnumerationNotComparable).ShouldBeTrue();
    [Fact] void should_not_claim_completeness() => _result.Complete.ShouldBeFalse();
}
