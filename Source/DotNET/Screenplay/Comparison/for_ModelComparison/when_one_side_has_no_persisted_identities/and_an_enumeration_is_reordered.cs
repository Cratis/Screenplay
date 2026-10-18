// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_one_side_has_no_persisted_identities;

public class and_an_enumeration_is_reordered : when_enumeration_members_change.given.an_enumeration
{
    void Establish() => Models(Positional, "paid\n  issued\n  draft");
    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithoutIdentities(_after));

    [Fact] void should_report_both_number_changes() => _result.Enumerations.Count.ShouldEqual(2);
    [Fact] void should_not_expose_generated_identities() => _result.Enumerations.All(change => change.Declaration.SemanticId is null).ShouldBeTrue();
    [Fact] void should_report_number_changes_on_the_matched_address() => _result.Enumerations.All(change => change.Change == EnumerationMemberChangeKind.NumberChanged && change.Declaration.BeforeAddress == "Status" && change.Declaration.AfterAddress == "Status").ShouldBeTrue();
}
