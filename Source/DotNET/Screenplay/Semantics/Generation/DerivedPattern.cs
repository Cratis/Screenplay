// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Screenplay.Semantics.Generation;

internal static class DerivedPattern
{
    internal static string? Create(SemanticGeneration? generation)
    {
        var shape = GenerationShape.Create(generation);
        if (shape is null) return null;

        return "^" + Escape(shape.Prefix) + string.Concat(shape.Runs.Where(run => run.Count > 0).Select(run => run.Pattern + (run.Count == 1 ? string.Empty : "{" + run.Count.ToString(CultureInfo.InvariantCulture) + "}"))) + "$";
    }

    internal static string Escape(string text) => string.Concat(text.Select(character => "\\^$.|?*+()[]{}".Contains(character, StringComparison.Ordinal) ? "\\" + character : character.ToString()));

    internal static string CharacterClass(string characters) => "[" + string.Concat(characters.Select(character => "\\^-[]".Contains(character, StringComparison.Ordinal) ? "\\" + character : character.ToString())) + "]";
}
