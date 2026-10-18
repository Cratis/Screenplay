// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_enumeration_members_change;

public class and_explicit_numbers_are_kept_while_reordering : given.an_enumeration
{
    void Establish() => Models(Explicit, "paid = 7\n  issued = 4\n  draft = 1");
    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithIdentities(_after));

    [Fact] void should_not_report_changed_numeric_identities() => _result.Enumerations.ShouldBeEmpty();
    [Fact] void should_compare_bound_models() => _result.AfterExecutable.ShouldBeTrue();
}
