// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_MatchAmbiguity;

public class when_detecting_exponential_ambiguity
{
    [Theory]
    [InlineData("(a+)+$")]
    [InlineData("(a|a)*")]
    [InlineData("(a*)*")]
    [InlineData(@"(\w|\d)+")]
    [InlineData("([a-z]+)*")]
    [InlineData("(a*|b)*")]
    [InlineData("(ab|ab)*")]
    [InlineData("(a|aa)*")]
    [InlineData("(?:[a-c]|[c-e])+")]
    [InlineData(@"(\s|\u00a0)+")]
    [InlineData(@"(\D|x)+")]
    [InlineData("(?<name>a+)+?")]
    [InlineData("(a{1,2})*")]
    [InlineData("(a{0,2})+")]

    // Nonempty iterations can split aaa as a{3} or a{1} followed by a{2}.
    [InlineData("(a{1,3})+")]
    [InlineData("(a+)+[^\\s\\S]|(b+)+")]
    public void should_find_two_synchronized_loop_derivations(string pattern) => MatchAmbiguity.Find(pattern).ShouldNotBeNull();

    [Theory]
    [InlineData("a+")]
    [InlineData("a*")]
    [InlineData("(ab)+")]
    [InlineData("[a-z]+@[a-z]+")]
    [InlineData("(a|b)*")]
    [InlineData(@"\d{3}-\d{4}")]

    // Every b determines its preceding a: no two same-word closed paths exist.
    [InlineData("(a|ab)*")]
    [InlineData("^[A-Za-z_][A-Za-z0-9_]*$")]
    [InlineData(SemanticMatchPattern.Email)]
    [InlineData("(a|a){3}")]
    [InlineData("a$(a+)+")]
    [InlineData("(a+)+^b")]
    [InlineData("[^\\s\\S](a+)+")]
    [InlineData("(a+)+[^\\s\\S]")]
    [InlineData("((a+)+){0}")]
    [InlineData("(a^)*")]
    [InlineData("(a|$)*")]
    [InlineData("(^|a)*")]
    [InlineData("([^a]|a)*")]
    [InlineData("(a{2})*")]
    [InlineData("(a{2})+")]
    [InlineData("(a?)*")]
    [InlineData("(a|)*")]
    [InlineData("(ab{0,2})+$")]
    [InlineData("(ab{1,3})+$")]
    [InlineData("(a+)+a{256}")]
    [InlineData("((){3000}){3000}(a+)+")]
    public void should_not_warn_without_a_trimmed_ambiguous_cycle(string pattern) => MatchAmbiguity.Find(pattern).ShouldBeNull();

    [Theory]
    [InlineData("(a+)+$", "a+")]
    [InlineData("(a|a)*", "a*")]
    [InlineData("(a*)*", "a*")]
    [InlineData("(a+)*", "a*")]
    public void should_suggest_a_simple_equivalent(string pattern, string suggestion) => MatchAmbiguity.Find(pattern)!.Suggestion.ShouldEqual(suggestion);

    [Fact] public void should_name_the_outer_repetition() => MatchAmbiguity.Find("^x(a+)+$")!.Subexpression.ShouldEqual("(a+)+");
    [Fact] public void should_use_utf16_offsets() => MatchAmbiguity.Find("😀(a+)+$")!.Offset.ShouldEqual(2);
    [Fact] public void should_omit_an_unproven_rewrite() => MatchAmbiguity.Find(@"(\w|\d)+")!.Suggestion.ShouldBeNull();
    [Fact] public void should_omit_a_duplicate_alternative_rewrite_that_remains_ambiguous() => MatchAmbiguity.Find("(a+|a+)*")!.Suggestion.ShouldBeNull();
    [Fact] public void should_find_a_nonempty_cycle_after_a_group_anchor() => MatchAmbiguity.Find("(^(a+)+)$")!.Offset.ShouldEqual(2);
}
