// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Generation;

internal sealed record GenerationShape(string Prefix, IReadOnlyList<GenerationRun> Runs)
{
    internal string[] Positions => [.. Prefix.Select(character => character.ToString()), .. Runs.SelectMany(run => Enumerable.Repeat(new string([.. run.Characters.Distinct().Order()]), run.Count))];

    internal static GenerationShape? Create(SemanticGeneration? generation)
    {
        generation ??= new("uuid");
        var prefix = generation.Prefix ?? string.Empty;
        var runs = GenerationShapes.Runs(generation);
        if (generation.Kind == "typeid" && prefix.Length > 0) prefix += "_";

        return runs is null ? null : new(prefix, runs);
    }

    internal IEnumerable<string> Samples()
    {
        var positions = Positions;
        yield return new([.. positions.Select(characters => characters[0])]);
        yield return new([.. positions.Select(characters => characters[^1])]);
        yield return new([.. positions.Select((characters, index) => index % 2 == 0 ? characters[0] : characters[^1])]);
    }
}
