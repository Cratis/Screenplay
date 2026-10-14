// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Screenplay.Semantics.Generation.for_RuleConsistency;

public class when_cross_checking_a_proof : Specification
{
    readonly GenerationShape _shape = GenerationShape.Create(new("nanoid") { Size = 2, Alphabet = "ab" })!;

    [Fact]
    void should_refute_a_runtime_divergence_with_the_failing_sample()
    {
        var result = RuleConsistency.CrossCheck(_shape, RuleConsistencyResult.Proven, value => value != "ab");
        result.Status.ShouldEqual(RuleConsistencyStatus.Refuted);
        result.Witness.ShouldEqual("ab");
    }

    [Fact]
    void should_not_report_proven_after_a_runtime_timeout()
    {
        var result = RuleConsistency.CrossCheck(_shape, RuleConsistencyResult.Proven, _ => throw new RegexMatchTimeoutException());
        result.Status.ShouldEqual(RuleConsistencyStatus.Unproven);
        result.Reason.ShouldEqual("runtime regex timed out");
    }

    [Fact]
    void should_cross_check_an_in_fragment_backtracking_pattern()
    {
        var result = RuleConsistency.Check(new("nanoid") { Size = 256, Alphabet = "ab" }, SemanticPrimitiveType.Text, new(default, SemanticValidationRuleKind.Matches, SemanticValue.Text("^(a+)+b$|^[ab]+$"), null));
        result.Status.ShouldEqual(RuleConsistencyStatus.Unproven);
        result.Reason.ShouldEqual("runtime regex timed out");
    }
}
