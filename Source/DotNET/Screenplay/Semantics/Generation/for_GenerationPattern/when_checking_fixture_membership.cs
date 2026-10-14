// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Generation.for_GenerationPattern;

public class when_checking_fixture_membership : Specification
{
    JsonElement[] _vectors;
    bool[] _matches;

    void Establish() => _vectors = given.shared_generation_vectors.Read("membership");
    void Because() => _matches = [.. _vectors.Select(vector => GenerationPatternSupport.Matches(given.shared_generation_vectors.Pattern(vector), vector.GetProperty("value").GetString()!, given.shared_generation_vectors.Text(vector, "instant") is { } instant ? DateTimeOffset.Parse(instant, CultureInfo.InvariantCulture) : null))];

    [Fact] void should_use_one_utc_instant_or_digit_widths_without_a_clock() => _matches.ShouldEqual([.. _vectors.Select(vector => vector.GetProperty("valid").GetBoolean())]);
}
