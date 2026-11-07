// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

internal static partial class ScreenParser
{
    /// <summary>
    /// What a <c>show</c> line may hold, as it reads in a diagnostic.
    /// </summary>
    internal const string ShowExpected = "show <Screen> by <param> from <element>.selection";

    static ScreenShowSyntax? ParseShow(ParserContext context, SourceLine line)
    {
        context.SkipBlock(line.Indent);
        var match = ShowRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidScreenShow, $"Invalid show '{line.Content}' - expected '{ShowExpected}'", line.Location);
            return null;
        }

        return new(match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value, line.Location);
    }

    [GeneratedRegex(@"^show\s+([A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)\s+by\s+([A-Za-z_]\w*)\s+from\s+([A-Za-z_][\w-]*)\.selection$", RegexOptions.NonBacktracking)]
    private static partial Regex ShowRegex();
}
