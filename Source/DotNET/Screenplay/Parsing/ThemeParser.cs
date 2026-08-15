// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>theme</c> declarations - a named visual theme and the component packages it is compatible with.
/// </summary>
internal static partial class ThemeParser
{
    /// <summary>
    /// Parses a theme from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding the <c>theme</c> header.</param>
    /// <returns>The parsed <see cref="ThemeSyntax"/>.</returns>
    public static ThemeSyntax Parse(ParserContext context, SourceLine header)
    {
        var match = HeaderRegex().Match(header.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidThemeDeclaration, $"Invalid theme declaration '{header.Content}' - expected 'theme <Name>'", header.Location);
            context.SkipBlock(header.Indent);
            return new(LineText.FirstWord(header.Content), [], header.Location);
        }

        var name = match.Groups[1].Value;
        var compatibleWith = new List<string>();
        var seen = new HashSet<string>();

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            var compatibleMatch = CompatibleWithRegex().Match(line.Content);
            if (!compatibleMatch.Success)
            {
                context.Error(DiagnosticCodes.InvalidCompatibleWithDeclaration, $"Invalid compatibility declaration '{line.Content}' - expected 'compatible with <Package>'", line.Location);
                continue;
            }

            var package = compatibleMatch.Groups[1].Value;
            if (!seen.Add(package))
            {
                context.Error(DiagnosticCodes.DuplicateCompatibleWith, $"Theme '{name}' already declares compatibility with '{package}'", line.Location);
                continue;
            }

            compatibleWith.Add(package);
        }

        return new(name, compatibleWith, header.Location);
    }

    [GeneratedRegex(@"^theme\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^compatible\s+with\s+([A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)$", RegexOptions.None, 1000)]
    private static partial Regex CompatibleWithRegex();
}
