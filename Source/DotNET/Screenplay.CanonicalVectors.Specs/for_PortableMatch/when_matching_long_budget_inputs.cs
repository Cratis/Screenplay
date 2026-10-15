// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET10_0
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_PortableMatch;

public class when_matching_long_budget_inputs
{
    [Fact]
    void should_agree_with_the_normalized_nonbacktracking_pattern_for_long_budget_edge_inputs()
    {
        // Node's default-engine oracle checks these risky shapes only on <= 6 code units under
        // a watchdog. Here .NET 10 NonBacktracking independently covers 100-1000 units; the
        // completion spec covers 10000 units. The structural state-bound spec needs no oracle.
        using var document = MatchVectors.Read("portable-match-differential.json");
        var patterns = document.RootElement.GetProperty("patterns").EnumerateArray()
            .Where(vector => vector.GetProperty("category").GetString() == "budget-edge")
            .Select(vector => vector.GetProperty("pattern").GetString()!)
            .Concat(document.RootElement.GetProperty("budgetCases").EnumerateArray()
                .Where(vector => vector.GetProperty("accepted").GetBoolean())
                .Select(vector => vector.GetProperty("pattern").GetString()!)).Distinct().ToArray();
        patterns.Length.ShouldBeGreaterThan(30);
        foreach (var pattern in patterns)
        {
            var parsed = SemanticMatchPattern.Parse(pattern);
            parsed.Accepted.ShouldBeTrue();
            var machine = SemanticMatchPattern.Create(parsed);
            var oracle = PortableMatchDotNetOracle.Compile(parsed);
            foreach (var length in new[] { 100, 500, 1000 })
            {
                foreach (var input in new string[] { new('a', length), new('x', length), new string('a', length - 1) + 'b', new('\ud800', length), string.Concat(Enumerable.Repeat("\ud83d\ude00", length / 2)) }) Assert.True(machine.IsMatch(input) == oracle(input), $"Long-input disagreement: pattern={pattern}; length={input.Length}");
            }
        }
    }
}
#endif
