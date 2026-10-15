// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_PortableMatch;

public class when_matching_boundaries
{
    [Theory]
    [InlineData("x{1500}", 1500)]
    [InlineData("a{3000}", 3000)]
    [InlineData("a{2998,}", 2998)]
    [InlineData("^a{2998}$", 2998)]
    [InlineData("^(?:a{2}){1499}$", 2998)]
    [InlineData("^a{2996,}$", 2996)]
    [InlineData("a{0,1500}", 1500)]
    [InlineData("(a*){1500}", 1500)]
    void should_evaluate_at_the_3000_node_budget_on_every_target_framework(string pattern, int length) =>
        SemanticMatchPattern.Create(pattern).IsMatch(new string(pattern[0] == 'x' ? 'x' : 'a', length)).ShouldBeTrue();

    [Theory]
    [InlineData("a{3001}")]
    [InlineData("a{2999,}")]
    [InlineData("^a{2999}$")]
    [InlineData("^(?:a{2}){1499}a$")]
    [InlineData("^a{2997,}$")]
    [InlineData("a{0,1501}")]
    [InlineData("(a*){1501}")]
    void should_refuse_more_than_3000_nodes_before_building_an_automaton(string pattern)
    {
        var result = SemanticMatchPattern.Parse(pattern);
        result.Accepted.ShouldBeFalse();
        result.Construct.ShouldEqual("expanded pattern size");
        result.Expression.ShouldBeNull();
    }

    [Theory]
    [InlineData("^a|b", "zzb", true)]
    [InlineData("a$|^b", "zza", true)]
    [InlineData("(^a|b$)", "zzb", true)]
    [InlineData("^a$", "ba", false)]
    [InlineData("$a", "a", false)]
    [InlineData("(a?)*", "", true)]
    [InlineData("^(a?)*$", "aaaa", true)]
    [InlineData("^(a?)*$", "aaab", false)]
    [InlineData("^((|a)*)*$", "aaaa", true)]
    [InlineData("a{2,4}b", "zzaaaabzz", true)]
    [InlineData("a{2,4}b", "zzabzz", false)]
    [InlineData("a{2,}b", "zzaaaaabzz", true)]
    [InlineData("^a{0,2}b$", "b", true)]
    [InlineData("^a{0,2}b$", "aaab", false)]
    void should_preserve_search_assertions_counts_and_epsilon_cycles(string pattern, string input, bool expected) =>
        SemanticMatchPattern.Create(pattern).IsMatch(input).ShouldEqual(expected);
}
