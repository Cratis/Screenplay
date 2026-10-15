// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Generation.for_GenerationChecksum;

public class when_reading_shared_checksum_vectors : Specification
{
    JsonElement[] _generation;
    JsonElement[] _validation;
    string?[] _suffixes;
    bool[] _matches;

    void Establish()
    {
        var vectors = Vectors();
        _generation = [.. vectors.GetProperty("checksums").EnumerateArray()];
        _validation = [.. vectors.GetProperty("validation").EnumerateArray()];
    }

    void Because()
    {
        _suffixes = [.. _generation.Select(vector => GenerationChecksum.Calculate(vector.GetProperty("payload").GetString()!, vector.GetProperty("algorithm").GetString()!))];
        _matches = [.. _validation.Select(vector => GenerationChecksum.Matches(vector.GetProperty("value").GetString()!, vector.GetProperty("algorithm").GetString()!))];
    }

    [Fact] void should_check_a_nonempty_generation_population() => _generation.ShouldNotBeEmpty();
    [Fact] void should_check_a_nonempty_validation_population() => _validation.ShouldNotBeEmpty();
    [Fact] void should_produce_every_expected_check_digit() => _suffixes.ShouldEqual(_generation.Select(vector => vector.GetProperty("suffix").GetString()).ToArray());
    [Fact] void should_validate_only_the_exact_trailing_check_digits() => _matches.ShouldEqual(_validation.Select(vector => vector.GetProperty("valid").GetBoolean()).ToArray());

    static JsonElement Vectors([CallerFilePath] string path = "")
    {
        var root = Directory.GetParent(path);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "AGENTS.md"))) root = root.Parent;
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root!.FullName, "Source", "Screenplay", "Compiler", "Conformance", "generation-checksums.json")));

        return document.RootElement.Clone();
    }
}
