// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

internal static partial class TemplateAssignmentParser
{
    public static TemplateAssignmentSyntax? Parse(ParserContext context, SourceLine line)
    {
        var match = TemplateRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.UnknownLayoutDirective, $"Invalid template assignment '{line.Content}' - expected 'template <Name>'", line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        if (context.TryPeekChild(line.Indent, out _))
        {
            context.Error(DiagnosticCodes.UnknownLayoutDirective, $"Unexpected block under template assignment '{line.Content}' - assign one template per line", line.Location);
            context.SkipBlock(line.Indent);
        }

        return new(match.Groups[1].Value, line.Location);
    }

    [GeneratedRegex(@"^template\s+([A-Za-z_]\w*(?:\.\w+)*)$", RegexOptions.None, 1000)]
    private static partial Regex TemplateRegex();
}
