// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Reads a single authoring-only fenced markdown documentation block.
/// </summary>
internal static class DocumentationParser
{
    internal static string? Parse(
        ParserContext context,
        SourceLine line,
        string? existing,
        string owner,
        IDictionary<string, SourceLocation> locations,
        string code = DiagnosticCodes.InvalidDocumentation)
    {
        if (line.Content != "documentation" || !context.TryPeekChild(line.Indent, out var fence) || fence.Content != "```markdown")
        {
            context.Error(code, $"{owner} documentation requires a fenced markdown block", line.Location);
            context.SkipBlock(line.Indent);
            return existing;
        }

        var text = CodeBlockParser.ParseFencedText(context, "markdown", line);
        if (string.IsNullOrWhiteSpace(text) || existing is not null)
        {
            context.Error(code, $"{owner} accepts one nonempty documentation block", line.Location);
            return existing;
        }

        locations["documentation"] = line.Location;
        return text;
    }
}
