// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Generation.for_GenerationPattern;

public class when_counting_distinct_support : Specification
{
    JsonElement[] _vectors;
    string[] _counts;

    void Establish() => _vectors = given.shared_generation_vectors.Read("support");
    void Because() => _counts = [.. _vectors.Select(vector => GenerationPatternSupport.Count(given.shared_generation_vectors.Pattern(vector), given.shared_generation_vectors.Text(vector, "date") is { } date ? DateOnly.Parse(date, CultureInfo.InvariantCulture) : null).ToString(CultureInfo.InvariantCulture))];

    [Fact] void should_count_every_distinct_string_after_checksum_conditioning() => _counts.ShouldEqual([.. _vectors.Select(vector => vector.GetProperty("count").GetString())]);
}
