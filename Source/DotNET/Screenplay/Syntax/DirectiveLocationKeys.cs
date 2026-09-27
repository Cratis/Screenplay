// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Identifies a scalar collection directive by its value and occurrence among identical values, not its list position.
/// </summary>
internal static class DirectiveLocationKeys
{
    internal static string ForValue(string directive, IReadOnlyList<string> values, int index)
    {
        var value = values[index];
        var occurrence = 0;
        for (var previous = 0; previous < index; previous++)
        {
            if (string.Equals(values[previous], value, StringComparison.Ordinal))
            {
                occurrence++;
            }
        }

        return $"{directive}:{value.Length}:{value}:{occurrence}";
    }

    internal static bool IsCollectionKey(string key) =>
        key.StartsWith("value:", StringComparison.Ordinal) ||
        key.StartsWith("policy:", StringComparison.Ordinal) ||
        key.StartsWith("compatible:", StringComparison.Ordinal) ||
        key.StartsWith("package:", StringComparison.Ordinal) ||
        key.StartsWith("role:", StringComparison.Ordinal) ||
        key.StartsWith("target:", StringComparison.Ordinal);
}
