// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Numerics;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Generation.for_GenerationPattern;

public class when_assessing_shared_date_vectors
{
    [Fact]
    void should_preserve_exact_support_and_first_empty_date_with_bounded_actual_work()
    {
        var vectors = given.shared_generation_vectors.Read("assessments");
        vectors.Length.ShouldEqual(10);
        foreach (var vector in vectors) Check(vector);
    }

    static void Check(JsonElement vector)
    {
        var budget = new GenerationWorkBudget(vector.GetProperty("budget").GetInt64());
        var pattern = given.shared_generation_vectors.Pattern(vector);
        var year = vector.GetProperty("year").GetInt32();
        if (given.shared_generation_vectors.Text(vector, "error") is { } reason)
        {
            var error = Catch.Exception(() => GenerationPatternAssessment.MinimumCount(pattern, year, budget));
            error.ShouldNotBeNull();
            error.Message.ShouldContain(reason, StringComparison.Ordinal);
        }
        else
        {
            GenerationPatternAssessment.MinimumCount(pattern, year, budget).ShouldEqual(BigInteger.Parse(vector.GetProperty("count").GetString()!, System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}
