// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses a command <c>calendar</c> declaration (decisions 0106 and 0107).
/// </summary>
internal static partial class CommandCalendarParser
{
    // A line that merely starts with the word is a property of that name unless it takes one of the two calendar forms.
    internal static bool IsDeclaration(string content) => PrefixRegex().IsMatch(content);

    internal static CommandCalendarSyntax? Parse(SourceLine line)
    {
        var match = DeclarationRegex().Match(line.Content);
        if (!match.Success) return null;
        return match.Groups[1].Success
            ? new(StringLiteral.Unescape(match.Groups[1].Value), null, line.Location)
            : new(null, match.Groups[2].Value, line.Location);
    }

    [GeneratedRegex("^calendar\\s+(\"|from\\s+\\$context\\.)", RegexOptions.NonBacktracking)]
    private static partial Regex PrefixRegex();

    [GeneratedRegex("^calendar\\s+(?:\"(" + StringLiteral.BodyPattern + ")\"|from\\s+\\$context\\.([A-Za-z_][A-Za-z0-9_]*(?:\\.[A-Za-z_][A-Za-z0-9_]*)*))$", RegexOptions.NonBacktracking)]
    private static partial Regex DeclarationRegex();
}
