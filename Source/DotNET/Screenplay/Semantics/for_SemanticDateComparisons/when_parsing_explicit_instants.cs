// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticDateComparisons;

public class when_parsing_explicit_instants : Specification
{
    (bool Parsed, DateTimeOffset Instant)[] _results;

    void Because() => _results = new[]
    {
        "2026-01-01T02:00:00.1234567+02:00",
        "2026-01-01T00:00:00.1234567Z",
        "2026-01-01T02:00+02:00",
        "2026-01-01T00:00Z",
        "2026-01-01T00:00:00Z",
        "2026-01-01T02:00:00+02:00",
        "2026-01-01T00:00:00.1Z",
        "2026-01-01T02:00:00+0200",
        "2026-01-01T02:00:00+2:00"
    }.Select(value => (SemanticDateComparisons.TryParseInstant(value, out var instant), instant)).ToArray();

    [Fact] void should_accept_all_four_formats_optional_fractions_and_offset_spellings() => _results.ShouldEachConformTo(result => result.Parsed);
    [Fact] void should_preserve_tick_precision() => _results[0].Instant.UtcTicks.ShouldEqual(_results[1].Instant.UtcTicks);
    [Fact] void should_preserve_offsets() => _results[0].Instant.Offset.ShouldEqual(TimeSpan.FromHours(2));
    [Fact] void should_parse_minute_precision_as_the_same_instant() => _results[2].Instant.UtcTicks.ShouldEqual(_results[3].Instant.UtcTicks);
}
