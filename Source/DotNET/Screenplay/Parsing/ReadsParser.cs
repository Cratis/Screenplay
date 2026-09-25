// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses the <c>reads</c> declaration naming a view a command or reaction trigger consults before it decides.
/// </summary>
internal static partial class ReadsParser
{
    /// <summary>
    /// Parses a <c>reads</c> declaration from its line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <param name="line">The <see cref="SourceLine"/> holding the declaration.</param>
    /// <returns>The parsed <see cref="ReadsSyntax"/>, or <c>null</c> when the line is malformed.</returns>
    public static ReadsSyntax? Parse(ParserContext context, SourceLine line)
    {
        var match = ReadsRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(
                DiagnosticCodes.InvalidReadsDeclaration,
                $"Invalid reads declaration '{line.Content}' - expected 'reads <ReadModel> [as <alias>] [by <property>]'",
                line.Location);
            return null;
        }

        var alias = match.Groups[2];
        if (alias.Success && (alias.Value == "as" || alias.Value == "by" || alias.Value == "reads"))
        {
            context.Error(
                DiagnosticCodes.InvalidReadsDeclaration,
                $"Invalid reads declaration '{line.Content}' - '{alias.Value}' cannot be used as a reads alias",
                line.Location);
            return null;
        }

        var by = match.Groups[3];
        return new(match.Groups[1].Value, by.Success ? by.Value : null, line.Location)
        {
            Alias = alias.Success ? alias.Value : null
        };
    }

    [GeneratedRegex(@"^reads\s+([A-Z]\w*)(?:\s+as\s+([a-z_]\w*))?(?:\s+by\s+([a-z_]\w*))?$", RegexOptions.None, 1000)]
    private static partial Regex ReadsRegex();
}
