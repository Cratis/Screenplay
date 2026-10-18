// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_enumeration_members_change;

public class and_a_member_is_inserted_before_existing_members : given.an_enumeration
{
    void Establish() => Models(Positional, "draft\n  pending\n  issued\n  paid");
    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithIdentities(_after));

    [Fact] void should_report_only_shifted_baseline_members() => _result.Enumerations.Select(change => change.Member).ShouldContainOnly("issued", "paid");
    [Fact] void should_report_renumbering() => _result.Enumerations.All(change => change.Change == EnumerationMemberChangeKind.NumberChanged && change.AfterNumber == change.BeforeNumber + 1).ShouldBeTrue();
}
