// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Generation.for_RuleConsistency;

public class when_comparing_small_languages_to_runtime
{
    [Theory]
    [InlineData("a")]
    [InlineData("^a|b$")]
    [InlineData("^a$|b")]
    [InlineData("^(ab|ba)?[ab]*$")]
    [InlineData("^(a?|b?){2}$")]
    [InlineData("^(a?b?){2,3}$")]
    [InlineData("^(a{0,2}){2,4}$")]
    [InlineData("^(ab?)*$")]
    [InlineData("^(a|ab)+$")]
    [InlineData("^(?:[ab]+|[cd]*)$")]
    [InlineData("^a{1,}$|^b{0,3}$")]
    [InlineData("^[^a-c]{1,4}$")]
    [InlineData("^\\W?\\w*$")]
    [InlineData("^\\s?\\S*$")]
    [InlineData("^\\t|\\n|\\r|\\f|\\v$")]
    [InlineData("^a{0}$")]
    [InlineData("^$")]
    [InlineData("$")]
    [InlineData("^")]
    [InlineData("|")]
    void should_agree_with_exhaustive_runtime_validation_and_witness_order(string pattern)
    {
        var regex = SemanticMatchPattern.Create(pattern);
        for (var length = 1; length <= 4; length++)
        {
            var generation = new SemanticGeneration("nanoid") { Size = length, Alphabet = "ba _" };
            var values = new[] { string.Empty }.AsEnumerable();
            for (var position = 0; position < length; position++) values = [.. values.SelectMany(prefix => " _ab".Select(character => prefix + character))];
            var witness = values.FirstOrDefault(value => !regex.IsMatch(value));
            var result = RuleConsistency.Check(generation, SemanticPrimitiveType.Text, new(default, SemanticValidationRuleKind.Matches, new SemanticTextValue(pattern), null));
            var expression = new RegexFragmentR(pattern, length, boundToShape: false).Parse();
            var deterministic = new GlushkovAutomaton(expression).IsDeterministic;
            result.Status.ShouldEqual(witness is not null ? RuleConsistencyStatus.Refuted : deterministic ? RuleConsistencyStatus.Proven : RuleConsistencyStatus.Unproven);
            result.Witness.ShouldEqual(witness);
            result.Reason.ShouldEqual(witness is null && !deterministic ? RegexContainment.AmbiguousPatternReason : null);
        }
    }
}
