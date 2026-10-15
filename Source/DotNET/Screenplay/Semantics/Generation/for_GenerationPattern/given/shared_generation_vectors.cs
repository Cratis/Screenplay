// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Generation.for_GenerationPattern.given;

internal static class shared_generation_vectors
{
    internal static string? Text(JsonElement vector, string name) => vector.TryGetProperty(name, out var value) ? value.GetString() : null;
    internal static GenerationPattern Pattern(JsonElement vector) => GenerationPattern.Parse(vector.GetProperty("pattern").GetString()!, Text(vector, "checksum"));

    internal static JsonElement[] Read(string group, [CallerFilePath] string path = "")
    {
        var root = Directory.GetParent(path);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "AGENTS.md"))) root = root.Parent;
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root!.FullName, "Source", "Screenplay", "Compiler", "Conformance", "generation-patterns.json")));
        var vectors = document.RootElement.GetProperty(group).EnumerateArray().Select(vector => vector.Clone()).ToArray();
        vectors.ShouldNotBeEmpty();

        return vectors;
    }
}
