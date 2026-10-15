// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticMatchPattern;

public class when_matching_shared_javascript_vectors
{
    [Fact]
    void should_equal_node_flagless_regexp_test_for_every_input()
    {
        using var document = PortableMatchVectors.Read("portable-match-semantics.json");
        var vectors = document.RootElement.GetProperty("vectors").EnumerateArray().ToArray();
        vectors.Length.ShouldBeGreaterThan(900);
        foreach (var group in vectors.GroupBy(vector => vector.GetProperty("pattern").GetString()!))
        {
            var regex = SemanticMatchPattern.Create(group.Key);
            foreach (var vector in group)
            {
                var input = vector.TryGetProperty("inputCodeUnits", out var units)
                    ? new string([.. units.EnumerateArray().Select(unit => (char)unit.GetInt32())])
                    : vector.GetProperty("input").GetString()!;
                regex.IsMatch(input).ShouldEqual(vector.GetProperty("expected").GetBoolean());
            }
        }
    }
}
