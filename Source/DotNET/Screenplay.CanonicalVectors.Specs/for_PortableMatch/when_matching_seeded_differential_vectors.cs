// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
#if NET10_0
using System.Text.RegularExpressions;
#endif
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_PortableMatch;

public class when_matching_seeded_differential_vectors
{
    [Fact]
    void should_equal_node_for_every_seeded_and_reference_case_on_every_target_framework() =>
        Check(parsed => SemanticMatchPattern.Create(parsed).IsMatch);

#if NET10_0
    [Fact]
    void should_equal_node_for_the_normalized_dotnet_10_renderer_pattern() =>
        Check(parsed => new Regex(parsed.NormalizedDotNetPattern, RegexOptions.NonBacktracking | RegexOptions.CultureInvariant, Regex.InfiniteMatchTimeout).IsMatch);
#endif

    static void Check(Func<MatchPatternResult, Func<string, bool>> compile)
    {
        using var document = MatchVectors.Read("portable-match-differential.json");
        var root = document.RootElement;
        root.GetProperty("seed").GetInt32().ShouldEqual(0x08000084);
        root.GetProperty("generatedPatterns").GetInt32().ShouldEqual(1500);
        var examined = 0;
        foreach (var entry in root.GetProperty("patterns").EnumerateArray())
        {
            var pattern = entry.GetProperty("pattern").GetString()!;
            var parsed = SemanticMatchPattern.Parse(pattern);
            Assert.True(parsed.Accepted, $"Generated pattern was not admitted: {pattern}: {parsed.Description}");
            var matches = compile(parsed);
            foreach (var vector in entry.GetProperty("cases").EnumerateArray())
            {
                var input = new string([.. vector.GetProperty("inputCodeUnits").EnumerateArray().Select(unit => (char)unit.GetInt32())]);
                var expected = vector.GetProperty("expected").GetBoolean();
                Assert.True(matches(input) == expected, $"Node disagreement: pattern={pattern}; input={JsonSerializer.Serialize(input)}; expected={expected}");
                examined++;
            }
        }
        examined.ShouldEqual(root.GetProperty("caseCount").GetInt32());
        examined.ShouldBeGreaterThan(10000);
    }
}
