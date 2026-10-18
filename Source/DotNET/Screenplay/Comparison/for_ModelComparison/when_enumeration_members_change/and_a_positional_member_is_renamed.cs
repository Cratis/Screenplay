// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_enumeration_members_change;

public class and_a_positional_member_is_renamed : given.an_enumeration
{
    void Establish() => Models(Positional, Positional.Replace("issued", "sent", StringComparison.Ordinal));
    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithIdentities(_after));

    [Fact] void should_report_number_reuse_instead_of_guessing_a_rename() => _result.Enumerations.Single().Change.ShouldEqual(EnumerationMemberChangeKind.NumberReused);
    [Fact] void should_name_the_new_holder() => Member("issued").AfterMember.ShouldEqual("sent");
    [Fact] void should_flag_the_ambiguous_change_as_breaking() => Member("issued").ContractBreaking.ShouldBeTrue();
}
