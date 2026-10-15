// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_PortableMatch;

// This project runs on net8.0, net9.0 and net10.0 in Release, unlike compiler-inline Debug specs.
public class when_matching_shared_vectors
{
    [Fact]
    void should_reproduce_all_1107_node_verdicts_on_every_target_framework()
    {
        using var document = MatchVectors.Read("portable-match-semantics.json");
        var vectors = document.RootElement.GetProperty("vectors").EnumerateArray().ToArray();
        vectors.Length.ShouldEqual(1107);
        foreach (var group in vectors.GroupBy(vector => vector.GetProperty("pattern").GetString()!))
        {
            var matcher = SemanticMatchPattern.Create(group.Key);
            foreach (var vector in group)
            {
                var input = vector.TryGetProperty("inputCodeUnits", out var units)
                    ? new string([.. units.EnumerateArray().Select(unit => (char)unit.GetInt32())])
                    : vector.GetProperty("input").GetString()!;
                matcher.IsMatch(input).ShouldEqual(vector.GetProperty("expected").GetBoolean());
            }
        }
    }

    [Fact]
    void should_construct_every_admitted_shared_dialect_case()
    {
        using var document = MatchVectors.Read("portable-match-dialect.json");
        var admitted = document.RootElement.GetProperty("cases").EnumerateArray().Where(vector => vector.GetProperty("accepted").GetBoolean()).ToArray();
        admitted.Length.ShouldBeGreaterThan(40);
        foreach (var vector in admitted) SemanticMatchPattern.Create(vector.GetProperty("pattern").GetString()!).StateCount.ShouldBeGreaterThan(0);
    }
}
