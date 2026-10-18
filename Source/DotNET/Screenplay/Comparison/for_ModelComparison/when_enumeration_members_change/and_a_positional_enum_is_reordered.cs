// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_enumeration_members_change;

public class and_a_positional_enum_is_reordered : given.an_enumeration
{
    void Establish() => Models(Positional, "paid\n  issued\n  draft");
    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithIdentities(_after));

    [Fact] void should_report_only_moved_members() => _result.Enumerations.Select(change => change.Member).ShouldContainOnly("draft", "paid");
    [Fact] void should_report_number_changes() => _result.Enumerations.All(change => change.Change == EnumerationMemberChangeKind.NumberChanged).ShouldBeTrue();
    [Fact] void should_report_the_old_number() => Member("draft").BeforeNumber.ShouldEqual(0);
    [Fact] void should_report_the_new_number() => Member("draft").AfterNumber.ShouldEqual(2);
    [Fact] void should_keep_the_member_name() => Member("draft").AfterMember.ShouldEqual("draft");
    [Fact] void should_report_both_positional_modes() => _result.Enumerations.All(change => change.BeforeNumbering == EnumerationNumbering.Positional && change.AfterNumbering == EnumerationNumbering.Positional).ShouldBeTrue();
    [Fact] void should_flag_breaking_stored_changes() => _result.Enumerations.All(change => change.ContractBreaking && change.Stored).ShouldBeTrue();
    [Fact] void should_retain_a_complete_bound_comparison() => _result.Complete.ShouldBeTrue();
    [Fact] void should_not_project_enum_records_as_generic_members() => _result.Members.Select(change => change.Member).ShouldNotContain("draft");
}
