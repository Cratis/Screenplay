// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;

namespace Cratis.Screenplay.Conformance;

internal static class SemanticLossJsonFields
{
    public static ImmutableArray<string> Diagnostics(JsonElement entry, string where, List<string> problems)
    {
        if (!entry.TryGetProperty("diagnostics", out var codes))
        {
            return [];
        }

        if (codes.ValueKind != JsonValueKind.Array)
        {
            problems.Add($"'diagnostics' in {where} must be an array of strings.");
            return [];
        }

        if (codes.EnumerateArray().Any(code => code.ValueKind != JsonValueKind.String))
        {
            problems.Add($"'diagnostics' in {where} must contain only strings.");
            return [];
        }

        return [.. codes.EnumerateArray().Select(code => code.GetString()!)];
    }

    public static string? Optional(JsonElement element, string property, string where, List<string> problems)
    {
        if (!element.TryGetProperty(property, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }

        problems.Add($"'{property}' in {where} must be a string when present.");
        return null;
    }
}
