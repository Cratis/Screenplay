// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_enumeration_members_change;

public class and_a_positional_enum_is_pinned_to_its_positions : given.an_enumeration
{
    void Establish() => Models(Positional, "draft = 0\n  issued = 1\n  paid = 2");
    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithIdentities(_after));

    [Fact] void should_not_report_changed_numeric_identities() => _result.Enumerations.ShouldBeEmpty();
    [Fact] void should_keep_the_authored_number_hash_change() => _result.Members.Any(change => change.Member == "valueNumbers").ShouldBeTrue();
}
