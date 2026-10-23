// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Matching.Specs.for_PortableMatcher;

public class when_matching_budget_boundaries : Specification
{
    [Fact]
    void should_construct_exactly_the_admitted_budget_cases()
    {
        using var document = MatchVectors.Read("portable-match-differential.json");
        var vectors = document.RootElement.GetProperty("budgetCases").EnumerateArray().ToArray();
        vectors.Length.ShouldEqual(48);
        foreach (var vector in vectors)
        {
            var error = Catch.Exception(() => PortableMatcher.Compile(vector.GetProperty("pattern").GetString()!));
            (error is null).ShouldEqual(vector.GetProperty("accepted").GetBoolean());
            if (error is not null)
            {
                error.ShouldBeOfExactType<InvalidPortableMatchPattern>();
                error.Message.ShouldContain($"expanded pattern size at offset {vector.GetProperty("offset").GetInt32()}.");
            }
        }
    }
}
