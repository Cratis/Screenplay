// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Generation.for_RuleConsistency;

public class when_requiring_deterministic_patterns : Specification
{
    [Theory]
    [InlineData("^ab(a+)+c$|^[ab]{256}$")]
    [InlineData("^[ab]{1,256}$")]
    [InlineData("^(a?b?){1000000}$")]
    [InlineData("^(a?)*$")]
    [InlineData("^(?:a*)*$")]
    [InlineData("^(?:[ab]?){2,256}$")]
    void should_not_prove_an_ambiguous_containment(string pattern)
    {
        var result = Check(pattern);
        result.Status.ShouldEqual(RuleConsistencyStatus.Unproven);
        result.Reason.ShouldEqual(RegexContainment.AmbiguousPatternReason);
        result.Witness.ShouldBeNull();
    }

    [Fact]
    void should_not_let_length_clipping_hide_an_authored_backtracking_branch()
    {
        var result = Check("^(ab(a+)+c){1000000}$|^[ab]{256}$");
        result.Status.ShouldEqual(RuleConsistencyStatus.Unproven);
        result.Reason.ShouldEqual("position budget exceeded");
    }

    [Theory]
    [InlineData("^[ab]{256}$")]
    [InlineData("^[ab]+$")]
    [InlineData("^(?:a|b){256}$")]
    [InlineData("^a.*$|^b.*$")]
    void should_keep_deterministic_containment_proven(string pattern) => Check(pattern).Status.ShouldEqual(RuleConsistencyStatus.Proven);

    [Fact]
    void should_keep_ambiguous_refutations_with_their_witness()
    {
        var result = RuleConsistency.Check(new("nanoid") { Size = 1, Alphabet = "ab" }, SemanticPrimitiveType.Text, new(default, SemanticValidationRuleKind.Matches, SemanticValue.Text("^a|a$"), null));
        result.Status.ShouldEqual(RuleConsistencyStatus.Refuted);
        result.Witness.ShouldEqual("b");
    }

    [Theory]
    [InlineData("^a|a$", false)]
    [InlineData("^ab(a+)+c$", false)]
    [InlineData("^(a|b){21}$", true)]
    [InlineData("^[a-z0-9]{21}$", true)]
    [InlineData("^(\\n|a)$", true)]
    void should_compare_first_and_follow_classes_on_printable_ascii(string pattern, bool deterministic) =>
        new GlushkovAutomaton(new RegexFragmentR(pattern, 256, boundToShape: false).Parse()).IsDeterministic.ShouldEqual(deterministic);

    static RuleConsistencyResult Check(string pattern) => RuleConsistency.Check(new("nanoid") { Size = 256, Alphabet = "ab" }, SemanticPrimitiveType.Text, new(default, SemanticValidationRuleKind.Matches, SemanticValue.Text(pattern), null));
}
