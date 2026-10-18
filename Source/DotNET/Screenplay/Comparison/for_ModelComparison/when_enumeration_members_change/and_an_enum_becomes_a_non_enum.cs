// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_enumeration_members_change;

public class and_an_enum_becomes_a_non_enum : given.an_enumeration
{
    void Establish() => ChangeSource(Model(Positional).Replace("concept Status : Enum\n  " + Positional, "concept Status : String", StringComparison.Ordinal));
    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithIdentities(_after));

    [Fact] void should_not_report_enum_identity_changes() => _result.Enumerations.ShouldBeEmpty();
    [Fact] void should_report_the_changed_concept_type() => _result.Members.Any(change => change.Declaration.BeforeAddress == "Status" && change.Member == "type").ShouldBeTrue();
}
