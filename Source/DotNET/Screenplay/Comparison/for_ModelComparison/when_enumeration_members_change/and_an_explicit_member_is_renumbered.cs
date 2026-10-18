// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_enumeration_members_change;

public class and_an_explicit_member_is_renumbered : given.an_enumeration
{
    void Establish() => Models(Explicit, Explicit.Replace("issued = 4", "issued = -4", StringComparison.Ordinal));
    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithIdentities(_after));

    [Fact] void should_report_one_number_change() => _result.Enumerations.Single().Change.ShouldEqual(EnumerationMemberChangeKind.NumberChanged);
    [Fact] void should_report_the_previous_number() => Member("issued").BeforeNumber.ShouldEqual(4);
    [Fact] void should_report_the_new_number() => Member("issued").AfterNumber.ShouldEqual(-4);
    [Fact] void should_report_both_explicit_modes() => _result.Enumerations.All(change => change.BeforeNumbering == EnumerationNumbering.Explicit && change.AfterNumbering == EnumerationNumbering.Explicit).ShouldBeTrue();
}
