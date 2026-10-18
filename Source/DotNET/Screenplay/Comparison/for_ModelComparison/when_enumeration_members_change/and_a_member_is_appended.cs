// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_enumeration_members_change;

public class and_a_member_is_appended : given.an_enumeration
{
    void Establish() => Models(Positional, Positional + "\n  archived");
    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithIdentities(_after));

    [Fact] void should_not_report_changed_numeric_identities() => _result.Enumerations.ShouldBeEmpty();
    [Fact] void should_still_report_the_authored_change() => _result.HasSemanticChange.ShouldEqual(true);
}
