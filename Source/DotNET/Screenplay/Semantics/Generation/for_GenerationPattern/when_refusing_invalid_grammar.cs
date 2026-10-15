// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Generation.for_GenerationPattern;

public class when_refusing_invalid_grammar : Specification
{
    JsonElement[] _vectors;
    Exception?[] _errors;

    void Establish() => _vectors = given.shared_generation_vectors.Read("invalid");
    void Because() => _errors = [.. _vectors.Select(vector => Catch.Exception(() => given.shared_generation_vectors.Pattern(vector)))];

    [Fact]
    void should_name_each_actionable_grammar_failure()
    {
        for (var index = 0; index < _vectors.Length; index++)
        {
            _errors[index].ShouldBeOfExactType<InvalidGenerationPattern>();
            _errors[index]!.Message.ShouldContain(_vectors[index].GetProperty("reason").GetString()!, StringComparison.Ordinal);
        }
    }
}
