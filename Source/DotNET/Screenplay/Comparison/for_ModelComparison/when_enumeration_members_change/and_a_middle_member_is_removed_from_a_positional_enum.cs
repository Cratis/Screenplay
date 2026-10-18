// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_enumeration_members_change;

public class and_a_middle_member_is_removed_from_a_positional_enum : given.an_enumeration
{
    void Establish() => Models(Positional, "draft\n  paid");
    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithIdentities(_after));

    [Fact] void should_report_both_affected_members() => _result.Enumerations.Count.ShouldEqual(2);
    [Fact] void should_report_the_removed_members_number_as_reused() => Member("issued").Change.ShouldEqual(EnumerationMemberChangeKind.NumberReused);
    [Fact] void should_name_its_new_holder() => Member("issued").AfterMember.ShouldEqual("paid");
    [Fact] void should_report_the_survivors_renumbering() => Member("paid").Change.ShouldEqual(EnumerationMemberChangeKind.NumberChanged);
    [Fact] void should_report_the_survivors_new_number() => Member("paid").AfterNumber.ShouldEqual(1);
}
