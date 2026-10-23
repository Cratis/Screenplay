// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Matching.Specs.for_PortableMatcher;

internal static class MatchVectors
{
    internal static JsonDocument Read(string name)
    {
        var assembly = typeof(MatchVectors).Assembly;
        var resource = assembly.GetManifestResourceNames().Single(path => path.EndsWith(name, StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(resource)!;

        return JsonDocument.Parse(stream);
    }

    internal static string Input(JsonElement vector) => vector.TryGetProperty("inputCodeUnits", out var units)
        ? new string([.. units.EnumerateArray().Select(unit => (char)unit.GetInt32())])
        : vector.GetProperty("input").GetString()!;
}
