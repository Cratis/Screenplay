// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_one_side_has_no_persisted_identities;

public class and_an_unbound_enumeration_is_reordered : when_enumeration_members_change.given.an_enumeration
{
    void Establish() => ChangeSource(Model("paid\n  issued\n  draft") + "    slice\n");
    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithoutIdentities(_after));

    [Fact] void should_report_both_number_changes_without_a_bound_model() => _result.Enumerations.Count.ShouldEqual(2);
    [Fact] void should_report_declaration_level_fallback() => _result.Sections.SelectMany(section => section.Gaps).Any(gap => gap.Kind == ComparisonGapKind.DeclarationLevelOnly).ShouldBeTrue();
    [Fact] void should_not_expose_identities_in_fallback() => _result.Enumerations.All(change => change.Declaration.SemanticId is null).ShouldBeTrue();
}
