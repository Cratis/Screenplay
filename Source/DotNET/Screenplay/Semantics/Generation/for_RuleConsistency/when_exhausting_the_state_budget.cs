// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Generation.for_RuleConsistency;

public class when_exhausting_the_state_budget : Specification
{
    RuleConsistencyResult _result;

    void Because() => _result = RuleConsistency.Check(new("nanoid") { Size = 60, Alphabet = "ab" }, SemanticPrimitiveType.Text, new(default, SemanticValidationRuleKind.Matches, new SemanticTextValue("^[ab]*a[ab]{24}$"), null));

    [Fact] void should_stop_without_claiming_a_refutation() => _result.Status.ShouldEqual(RuleConsistencyStatus.Unproven);
    [Fact] void should_name_the_exhausted_budget() => _result.Reason.ShouldEqual("budget exceeded");
    [Fact] void should_not_report_a_witness() => _result.Witness.ShouldBeNull();
}
