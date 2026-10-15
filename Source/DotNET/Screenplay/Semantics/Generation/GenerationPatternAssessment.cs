// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Numerics;

namespace Cratis.Screenplay.Semantics.Generation;

internal static class GenerationPatternAssessment
{
    internal static BigInteger MinimumCount(GenerationPattern pattern, int utcYear, GenerationWorkBudget? budget = null)
    {
        budget ??= new();
        if (!pattern.HasDates)
        {
            var count = GenerationPatternSupport.Count(pattern, budget: budget);
            if (count.IsZero) throw new EmptyGenerationSupport(null, pattern.Checksum);

            return count;
        }
        if (utcYear is < 1 or > 9899) throw new InvalidSemanticContract("Generation assessment year must allow the complete 100-year UTC horizon.");
        var last = new DateOnly(utcYear + 100, 12, 31);
        var date = new DateOnly(utcYear, 1, 1);
        BigInteger? minimum = null;
        var counts = new Dictionary<string, BigInteger>(StringComparer.Ordinal);
        var classes = new GenerationDateClasses(pattern);
        while (true)
        {
            budget.Spend();
            var key = classes.Key(date, budget);
            if (!counts.TryGetValue(key, out var count))
            {
                count = GenerationPatternSupport.Count(pattern, date, budget);
                counts.Add(key, count);
            }
            if (count.IsZero) throw new EmptyGenerationSupport(date, pattern.Checksum);
            if (minimum is null || count < minimum.Value) minimum = count;
            if (date == last) break;
            date = date.AddDays(1);
        }

        return minimum.Value;
    }
}
