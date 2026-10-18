// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_enumeration_members_change;

public class and_the_enum_is_not_stored : given.an_enumeration
{
    void Establish() => ChangeSource(Model(Positional).Replace("first\n  second", "second\n  first", StringComparison.Ordinal));
    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithIdentities(_after));

    [Fact] void should_report_changes_outside_event_storage() => _result.Enumerations.Count.ShouldEqual(2);
    [Fact] void should_not_mark_the_enum_as_stored() => _result.Enumerations.Any(change => change.Stored).ShouldBeFalse();
    [Fact] void should_keep_breaking_classification_independent_of_storage() => _result.Enumerations.All(change => change.ContractBreaking).ShouldBeTrue();
}
