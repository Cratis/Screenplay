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
    void should_not_convert_an_unexpected_engine_failure_into_a_verdict() =>
        Catch.Exception(() => RuleConsistency.CrossCheck(_shape, RuleConsistencyResult.Proven, _ => throw new RegexMatchTimeoutException())).ShouldBeOfExactType<RegexMatchTimeoutException>();

    [Fact]
    void should_refuse_to_prove_an_ambiguous_in_fragment_pattern()
    {
        var result = RuleConsistency.Check(new("nanoid") { Size = 256, Alphabet = "ab" }, SemanticPrimitiveType.Text, new(default, SemanticValidationRuleKind.Matches, SemanticValue.Text("^(a+)+b$|^[ab]+$"), null));
        result.Status.ShouldEqual(RuleConsistencyStatus.Unproven);
        result.Reason.ShouldEqual(RegexContainment.AmbiguousPatternReason);
    }
}
