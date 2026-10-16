// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticDateComparisons;

public class when_parsing_invalid_instants : Specification
{
    bool[] _results;

    void Because() => _results = new[]
    {
        "2026-01-01T00:00:00",
        "2026-01-01T00:00",
        "2026-01-01",
        "not an instant",
        "2026-01-01T00:00:00.12345678Z",
        "2026-01-01T00:00.1Z",
        "2026-01-01T02:00:00+02"
    }.Select(value => SemanticDateComparisons.TryParseInstant(value, out _)).ToArray();

    [Fact] void should_reject_every_non_admitted_spelling() => _results.ShouldEachConformTo(result => !result);
}
