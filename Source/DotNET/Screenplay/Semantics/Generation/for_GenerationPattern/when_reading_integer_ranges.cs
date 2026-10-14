// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Screenplay.Semantics.Generation.for_GenerationPattern;

public class when_reading_integer_ranges
{
    [Fact]
    void should_preserve_exact_int64_endpoints_and_cardinality()
    {
        foreach (var vector in given.shared_generation_vectors.Read("ranges"))
        {
            var text = vector.GetProperty("range").GetString()!;
            if (vector.TryGetProperty("invalid", out _)) Catch.Exception(() => GenerationIntegerRange.Parse(text)).ShouldBeOfExactType<InvalidSemanticContract>();
            else GenerationIntegerRange.Parse(text).Count.ToString(CultureInfo.InvariantCulture).ShouldEqual(vector.GetProperty("count").GetString());
        }
    }
}
