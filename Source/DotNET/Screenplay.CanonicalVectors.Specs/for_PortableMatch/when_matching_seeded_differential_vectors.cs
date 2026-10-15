// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_PortableMatch;

public class when_matching_seeded_differential_vectors
{
    [Fact]
    void should_equal_node_for_every_seeded_and_reference_case_on_every_target_framework() =>
        Check(parsed =>
        {
            var machine = SemanticMatchPattern.Create(parsed);
            machine.StateCount.ShouldBeLessThanOrEqual(parsed.ExpandedSize + 1);
            machine.StateCount.ShouldBeLessThanOrEqual(2 * Math.Max(1, parsed.ExpandedSize));
            return machine.IsMatch;
        });

#if NET10_0
    [Fact]
    void should_equal_node_for_the_normalized_dotnet_10_renderer_pattern() =>
        Check(PortableMatchDotNetOracle.Compile);
#endif

    static void Check(Func<MatchPatternResult, Func<string, bool>> compile)
    {
        using var document = MatchVectors.Read("portable-match-differential.json");
        var root = document.RootElement;
        root.GetProperty("seed").GetInt32().ShouldEqual(0x08000084);
        root.GetProperty("generatedPatterns").GetInt32().ShouldEqual(1500);
        var examined = 0;
        var budgetExamined = 0;
        foreach (var entry in root.GetProperty("patterns").EnumerateArray())
        {
            entry.GetProperty("engine").GetString().ShouldEqual("node-default");
            var budgetEdge = entry.GetProperty("category").GetString() == "budget-edge";
            var pattern = entry.GetProperty("pattern").GetString()!;
            var parsed = SemanticMatchPattern.Parse(pattern);
            Assert.True(parsed.Accepted, $"Generated pattern was not admitted: {pattern}: {parsed.Description}");
            var matches = compile(parsed);
            foreach (var vector in entry.GetProperty("cases").EnumerateArray())
            {
                var input = new string([.. vector.GetProperty("inputCodeUnits").EnumerateArray().Select(unit => (char)unit.GetInt32())]);
                if (budgetEdge)
                {
                    input.Length.ShouldBeLessThanOrEqual(6);
                    budgetExamined++;
                }
                var expected = vector.GetProperty("expected").GetBoolean();
                Assert.True(matches(input) == expected, $"Node disagreement: pattern={pattern}; input={JsonSerializer.Serialize(input)}; expected={expected}");
                examined++;
            }
        }
        examined.ShouldEqual(root.GetProperty("caseCount").GetInt32());
        examined.ShouldBeGreaterThan(10000);
        budgetExamined.ShouldEqual(root.GetProperty("budgetEdgeCases").GetInt32());
        budgetExamined.ShouldBeGreaterThanOrEqual(300);
        (examined - budgetExamined).ShouldEqual(root.GetProperty("generalCases").GetInt32());
        var skipped = root.GetProperty("skipped").EnumerateArray().ToArray();
        skipped.Length.ShouldBeLessThanOrEqual((int)((examined + skipped.Length) * 0.05));
        var budgetSkipped = skipped.Count(vector => vector.GetProperty("category").GetString() == "budget-edge");
        budgetSkipped.ShouldBeLessThanOrEqual((int)((budgetExamined + budgetSkipped) * 0.05));
    }
}
