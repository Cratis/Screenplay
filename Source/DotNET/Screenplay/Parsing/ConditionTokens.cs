// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Reads condition tokens without silently dropping text between matches.
/// </summary>
internal static class ConditionTokens
{
    /// <summary>
    /// Tokenizes the complete input, reporting the first unrecognized fragment.
    /// </summary>
    /// <param name="context">The parsing context.</param>
    /// <param name="text">The complete condition text.</param>
    /// <param name="pattern">The tokens admitted by the owning grammar.</param>
    /// <param name="location">The condition location.</param>
    /// <param name="diagnostic">The owning grammar's diagnostic code.</param>
    /// <param name="subject">The condition's name in diagnostics.</param>
    /// <returns>The tokens, or null when non-whitespace text is unmatched.</returns>
    public static List<string>? Read(ParserContext context, string text, Regex pattern, SourceLocation location, string diagnostic, string subject)
    {
        var tokens = new List<string>();
        var end = 0;
        foreach (Match match in pattern.Matches(text))
        {
            if (ReportGap(context, text[end..match.Index], location, diagnostic, subject))
            {
                return null;
            }

            tokens.Add(match.Value);
            end = match.Index + match.Length;
        }

        return ReportGap(context, text[end..], location, diagnostic, subject) ? null : tokens;
    }

    static bool ReportGap(ParserContext context, string gap, SourceLocation location, string diagnostic, string subject)
    {
        if (string.IsNullOrWhiteSpace(gap))
        {
            return false;
        }

        context.Error(diagnostic, $"Unexpected '{gap.Trim()}' in {subject}", location);
        return true;
    }
}
