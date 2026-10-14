// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Numerics;

namespace Cratis.Screenplay.Semantics.Generation.for_GenerationPattern;

public class when_assessing_the_date_horizon
{
    [Fact]
    void should_scan_every_valid_date_including_leap_days_and_the_last_year()
    {
        var pattern = GenerationPattern.Parse("{MM}[AB]");
        var countingOperations = 0L;
        for (var month = 1; month <= 12; month++)
        {
            var budget = new GenerationWorkBudget();
            GenerationPatternSupport.Count(pattern, new(2020, month, 1), budget).ShouldEqual(new BigInteger(2));
            countingOperations += budget.Used;
        }
        var assessmentBudget = new GenerationWorkBudget();
        GenerationPatternAssessment.MinimumCount(pattern, 2020, assessmentBudget).ShouldEqual(new BigInteger(2));
        (assessmentBudget.Used - countingOperations).ShouldEqual(36_890L);
    }

    [Fact]
    void should_refuse_any_empty_date_support_with_the_offending_date()
    {
        var error = Catch.Exception(() => GenerationPatternAssessment.MinimumCount(GenerationPattern.Parse("{yyyy}[AB]", "mod11"), 2026));
        error.ShouldBeOfExactType<EmptyGenerationSupport>();
        error.Message.ShouldContain("UTC 2029-01-01", StringComparison.Ordinal);
    }

    [Fact]
    void should_never_count_date_shape_digits_as_entropy() => Catch.Exception(() => GenerationPatternSupport.Count(GenerationPattern.Parse("{yyyy}#"))).ShouldBeOfExactType<InvalidSemanticContract>();
}
