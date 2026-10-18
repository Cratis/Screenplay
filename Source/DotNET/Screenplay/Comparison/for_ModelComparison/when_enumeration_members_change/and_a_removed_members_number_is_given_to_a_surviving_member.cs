// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_enumeration_members_change;

public class and_a_removed_members_number_is_given_to_a_surviving_member : given.an_enumeration
{
    void Establish() => Models(Explicit, "draft = 1\n  paid = 4");
    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithIdentities(_after));

    [Fact] void should_report_both_affected_members() => _result.Enumerations.Count.ShouldEqual(2);
    [Fact] void should_not_claim_a_surviving_name_is_a_rename() => Member("issued").Change.ShouldEqual(EnumerationMemberChangeKind.NumberReused);
    [Fact] void should_name_the_holder() => Member("issued").AfterMember.ShouldEqual("paid");
    [Fact] void should_report_renumbering_of_the_holder() => Member("paid").Change.ShouldEqual(EnumerationMemberChangeKind.NumberChanged);
    [Fact] void should_report_both_as_breaking() => _result.Enumerations.All(change => change.ContractBreaking).ShouldBeTrue();
}
