// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

internal static partial class ObserverFilterParser
{
    internal static ObserverFilterSyntax? Parse(ParserContext context, SourceLine line, ObserverFilterSyntax? previous)
    {
        var match = HeaderRegex().Match(line.Content);
        if (!match.Success || previous is not null)
        {
            context.Error(DiagnosticCodes.InvalidObserverFilter, "Declare at most one 'from <Source>[.<Stream>]' observer filter.", line.Location);
        }
        else
        {
            previous = new(match.Groups[1].Value, match.Groups[2].Success ? match.Groups[2].Value : null, line.Location);
        }
        if (context.TryPeekChild(line.Indent, out var child))
        {
            context.Error(DiagnosticCodes.InvalidObserverFilter, "An observer filter cannot have children or filter stream ids.", child.Location);
            context.SkipBlock(line.Indent);
        }

        return previous;
    }

    [GeneratedRegex(@"^from\s+([A-Za-z_]\w*)(?:\.([A-Za-z_]\w*))?$", RegexOptions.None, 1000)]
    private static partial Regex HeaderRegex();
}
