// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Matching.Specs.for_PortableMatcher;

public class when_matching_shared_vectors : Specification
{
    [Fact]
    void should_reproduce_every_shared_node_verdict()
    {
        using var document = MatchVectors.Read("portable-match-semantics.json");
        var vectors = document.RootElement.GetProperty("vectors").EnumerateArray().ToArray();
        vectors.Length.ShouldEqual(1107);
        foreach (var group in vectors.GroupBy(vector => vector.GetProperty("pattern").GetString()!))
        {
            var matcher = PortableMatcher.Compile(group.Key);
            foreach (var vector in group) matcher.IsMatch(MatchVectors.Input(vector)).ShouldEqual(vector.GetProperty("expected").GetBoolean());
        }
    }

    [Fact]
    void should_reproduce_every_seeded_differential_case()
    {
        using var document = MatchVectors.Read("portable-match-differential.json");
        var root = document.RootElement;
        root.GetProperty("generatedPatterns").GetInt32().ShouldEqual(1500);
        var examined = 0;
        foreach (var entry in root.GetProperty("patterns").EnumerateArray())
        {
            var matcher = PortableMatcher.Compile(entry.GetProperty("pattern").GetString()!);
            foreach (var vector in entry.GetProperty("cases").EnumerateArray())
            {
                matcher.IsMatch(MatchVectors.Input(vector)).ShouldEqual(vector.GetProperty("expected").GetBoolean());
                examined++;
            }
        }
        examined.ShouldEqual(root.GetProperty("caseCount").GetInt32());
        examined.ShouldBeGreaterThan(10000);
    }
}
