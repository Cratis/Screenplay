// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Reads what follows <c>template</c> in an assignment or a screen's template reference: an own template <c>&lt;Name&gt;</c>,
/// or a blueprint template <c>blueprint &lt;Blueprint&gt; &lt;Name&gt; [version "&lt;range&gt;"]</c>.
/// </summary>
internal static partial class TemplateReferenceText
{
    /// <summary>
    /// Tries to read a blueprint reference.
    /// </summary>
    /// <param name="text">The text after the <c>template</c> keyword, trimmed.</param>
    /// <param name="reference">The blueprint, template name and optional version range.</param>
    /// <returns><see langword="true"/> when the text is the blueprint form.</returns>
    public static bool TryParseBlueprint(string text, out (string Blueprint, string Name, string? Version) reference)
    {
        var match = BlueprintRegex().Match(text);
        if (!match.Success)
        {
            reference = default;
            return false;
        }

        reference = (match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Success ? StringLiteral.Unescape(match.Groups[3].Value) : null);
        return true;
    }

    /// <summary>
    /// Whether the text starts the blueprint form, whether or not it is well formed.
    /// </summary>
    /// <param name="text">The text after the <c>template</c> keyword, trimmed.</param>
    /// <returns><see langword="true"/> when the first word is <c>blueprint</c> and more follows.</returns>
    public static bool StartsBlueprint(string text) => text.StartsWith("blueprint ", StringComparison.Ordinal);

    [GeneratedRegex("^blueprint\\s+([A-Za-z_]\\w*(?:\\.\\w+)*)\\s+([A-Za-z_]\\w*(?:\\.\\w+)*)(?:\\s+version\\s+\"(" + StringLiteral.BodyPattern + ")\")?$", RegexOptions.None, 1000)]
    private static partial Regex BlueprintRegex();
}
