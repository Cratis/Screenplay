// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_enumeration_members_change;

public class and_a_member_is_renamed_with_its_number_kept : given.an_enumeration
{
    void Establish() => Models(Explicit, Explicit.Replace("issued", "sent", StringComparison.Ordinal));
    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithIdentities(_after));

    [Fact] void should_report_one_rename() => _result.Enumerations.Single().Change.ShouldEqual(EnumerationMemberChangeKind.Renamed);
    [Fact] void should_report_the_new_name() => Member("issued").AfterMember.ShouldEqual("sent");
    [Fact] void should_keep_the_explicit_number() => Member("issued").AfterNumber.ShouldEqual(Member("issued").BeforeNumber);
    [Fact] void should_not_flag_a_breaking_change() => Member("issued").ContractBreaking.ShouldBeFalse();
    [Fact] void should_retain_the_storage_usage() => Member("issued").Stored.ShouldBeTrue();
}
