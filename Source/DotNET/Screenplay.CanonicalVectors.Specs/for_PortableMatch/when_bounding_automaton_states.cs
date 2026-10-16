// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_PortableMatch;

public class when_bounding_automaton_states
{
    [Fact]
    void should_bound_real_states_for_boundaries_nested_nullable_repeats_maximum_nesting_assertions_and_surrogate_ranges()
    {
        using var document = MatchVectors.Read("portable-match-differential.json");
        var cases = document.RootElement.GetProperty("budgetCases").EnumerateArray().ToArray();
        cases.Length.ShouldBeGreaterThan(40);
        foreach (var vector in cases)
        {
            var pattern = vector.GetProperty("pattern").GetString()!;
            var parsed = SemanticMatchPattern.Parse(pattern);
            parsed.Accepted.ShouldEqual(vector.GetProperty("accepted").GetBoolean());
            if (!parsed.Accepted)
            {
                parsed.Construct.ShouldEqual("expanded pattern size");
                parsed.Offset.ShouldEqual(vector.GetProperty("offset").GetInt32());
                parsed.Expression.ShouldBeNull();
                continue;
            }
            var size = vector.GetProperty("expandedSize").GetInt32();
            parsed.ExpandedSize.ShouldEqual(size);
            var states = SemanticMatchPattern.Create(parsed).StateCount;
            states.ShouldBeLessThanOrEqual(size + 1);
            if (size == 0) states.ShouldEqual(1);
            else states.ShouldBeLessThanOrEqual(2 * size);
        }
    }
}
