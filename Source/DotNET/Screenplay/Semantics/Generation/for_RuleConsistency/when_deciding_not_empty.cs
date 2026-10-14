// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Semantics.Generation.for_RuleConsistency;

public class when_deciding_not_empty : Specification
{
    [Theory]
    [InlineData("", " a", RuleConsistencyStatus.Refuted, "   ")]
    [InlineData("  ", " a", RuleConsistencyStatus.Refuted, "     ")]
    [InlineData("x ", " a", RuleConsistencyStatus.Proven, null)]
    [InlineData("  ", "ab", RuleConsistencyStatus.Proven, null)]
    void should_decide_whether_the_shape_can_emit_an_all_space_value(string prefix, string alphabet, RuleConsistencyStatus status, string? witness)
    {
        var rule = new SemanticValidationRule(default, SemanticValidationRuleKind.NotEmpty, null, null);
        var result = RuleConsistency.Check(new("nanoid") { Size = 3, Prefix = prefix, Alphabet = alphabet }, SemanticPrimitiveType.Text, rule);
        result.Status.ShouldEqual(status);
        result.Witness.ShouldEqual(witness);
        if (witness is not null) SemanticValidationRules.Satisfies(rule, SemanticValue.Text(witness)).ShouldBeFalse();
    }
}
