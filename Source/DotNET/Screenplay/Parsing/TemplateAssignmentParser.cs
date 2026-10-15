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
        var text = line.Content["template".Length..].Trim();
        var blueprint = TemplateReferenceText.StartsBlueprint(text);
        var match = TemplateRegex().Match(line.Content);
        if (blueprint ? !TemplateReferenceText.TryParseBlueprint(text, out _) : !match.Success)
        {
            context.Error(
                DiagnosticCodes.UnknownLayoutDirective,
                $"Invalid template assignment '{line.Content}' - expected 'template <Name>' or 'template blueprint <Blueprint> <Name> [version \"<range>\"]'",
                line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        if (context.TryPeekChild(line.Indent, out _))
        {
            context.Error(DiagnosticCodes.UnknownLayoutDirective, $"Unexpected block under template assignment '{line.Content}' - assign one template per line", line.Location);
            context.SkipBlock(line.Indent);
        }

        if (blueprint && TemplateReferenceText.TryParseBlueprint(text, out var reference))
        {
            if (reference.Version is not null && !TemplateVersionRange.IsValid(reference.Version))
            {
                context.Error(
                    DiagnosticCodes.InvalidTemplateVersionRange,
                    $"The template version range '{reference.Version}' is not one Scene understands - use '*', an exact version or a ^, ~, >=, >, <= or < range such as \"^1.2\"",
                    line.Location);
            }

            return new(reference.Name, line.Location) { Blueprint = reference.Blueprint, Version = reference.Version };
        }

        return new(match.Groups[1].Value, line.Location);
    }

    [GeneratedRegex(@"^template\s+([A-Za-z_]\w*(?:\.\w+)*)$", RegexOptions.None, 1000)]
    private static partial Regex TemplateRegex();
}
