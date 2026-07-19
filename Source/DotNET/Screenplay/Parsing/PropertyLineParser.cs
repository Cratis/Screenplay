// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses property lines - a lowercase name followed by a type reference, such as <c>lines InvoiceLine[]</c>.
/// </summary>
internal static partial class PropertyLineParser
{
    /// <summary>
    /// Attempts to parse a property line.
    /// </summary>
    /// <param name="line">The <see cref="SourceLine"/> to parse.</param>
    /// <returns>The parsed <see cref="PropertySyntax"/>, or <c>null</c> when the line is not a property line.</returns>
    public static PropertySyntax? TryParse(SourceLine line)
    {
        var match = PropertyRegex().Match(line.Content);
        if (!match.Success)
        {
            return null;
        }

        return new(match.Groups[1].Value, ParseTypeRef(match.Groups[2].Value, line.Location), line.Location);
    }

    /// <summary>
    /// Parses a type reference with its optional <c>[]</c> and <c>?</c> suffixes.
    /// </summary>
    /// <param name="text">The type reference text.</param>
    /// <param name="location">The <see cref="SourceLocation"/> of the reference.</param>
    /// <returns>The parsed <see cref="TypeRefSyntax"/>.</returns>
    public static TypeRefSyntax ParseTypeRef(string text, SourceLocation location)
    {
        var isOptional = text.EndsWith('?');
        if (isOptional)
        {
            text = text[..^1];
        }

        var isCollection = text.EndsWith("[]", StringComparison.Ordinal);
        if (isCollection)
        {
            text = text[..^2];
        }

        return new(text, isCollection, isOptional, location);
    }

    [GeneratedRegex(@"^([a-z_]\w*)\s+([\w.]+(?:\[\])?\??)$", RegexOptions.None, 1000)]
    private static partial Regex PropertyRegex();
}
