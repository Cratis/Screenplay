// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses the application root <c>time zone</c> declaration (decisions 0106 and 0107).
/// </summary>
internal static partial class TimeZoneParser
{
    internal static bool IsDeclaration(string content) =>
        content.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Take(2).SequenceEqual(["time", "zone"], StringComparer.Ordinal);

    internal static TimeZoneSyntax? Parse(ParserContext context, SourceLine line)
    {
        var match = DeclarationRegex().Match(line.Content);
        context.SkipBlock(line.Indent);
        return match.Success ? new(StringLiteral.Unescape(match.Groups[2].Value), match.Groups[1].Success, line.Location) : null;
    }

    [GeneratedRegex("^time\\s+zone\\s+(configurable\\s+default\\s+)?\"(" + StringLiteral.BodyPattern + ")\"$", RegexOptions.NonBacktracking)]
    private static partial Regex DeclarationRegex();
}
