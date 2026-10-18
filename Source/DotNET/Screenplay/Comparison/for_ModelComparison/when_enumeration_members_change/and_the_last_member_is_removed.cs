// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_enumeration_members_change;

public class and_the_last_member_is_removed : given.an_enumeration
{
    void Establish() => Models(Positional, "draft\n  issued");
    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithIdentities(_after));

    [Fact] void should_report_one_removal() => _result.Enumerations.Single().Change.ShouldEqual(EnumerationMemberChangeKind.Removed);
    [Fact] void should_report_the_removed_number() => Member("paid").BeforeNumber.ShouldEqual(2);
    [Fact] void should_have_no_candidate_holder() => Member("paid").AfterMember.ShouldBeNull();
    [Fact] void should_have_no_candidate_number() => Member("paid").AfterNumber.ShouldBeNull();
}
