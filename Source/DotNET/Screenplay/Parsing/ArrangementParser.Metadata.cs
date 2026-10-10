// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses the template picker metadata and template content a screen or dialog template declares.
/// </summary>
internal static partial class ArrangementParser
{
    static readonly string[] _templateScopes = ["application", "module", "feature", "subfeature", "slice"];

    static void ParseMetadata(ParserContext context, SourceLine line, string keyword, TemplateMetadata metadata)
    {
        var directive = LineText.FirstWord(line.Content);
        if (keyword == "layout" && directive != "scopes")
        {
            context.Error(DiagnosticCodes.UnknownLayoutDirective, $"A layout cannot declare '{directive}' - display names, descriptions and slot content belong to screen and dialog templates", line.Location);
            context.SkipBlock(line.Indent);
            return;
        }

        switch (directive)
        {
            case "scopes":
                metadata.Scopes = ParseScopes(context, line) ?? metadata.Scopes;
                break;
            case "display":
                metadata.DisplayName = ParseText(context, line, "display") ?? metadata.DisplayName;
                break;
            case "description":
                metadata.Description = ParseText(context, line, "description") ?? metadata.Description;
                break;
            default:
                ParseContent(context, line, metadata);
                break;
        }
    }

    static List<string>? ParseScopes(ParserContext context, SourceLine line)
    {
        var text = line.Content["scopes".Length..].Trim();
        if (text == "none")
        {
            return [];
        }

        var scopes = text.Split(',', StringSplitOptions.TrimEntries).ToList();
        if (scopes.Count == 0 || scopes.Exists(scope => !_templateScopes.Contains(scope, StringComparer.Ordinal)))
        {
            context.Error(DiagnosticCodes.UnknownLayoutDirective, $"Invalid template scopes '{line.Content}' - expected 'scopes <application|module|feature|subfeature|slice>, ...' or 'scopes none'", line.Location);
            return null;
        }

        return scopes;
    }

    static string? ParseText(ParserContext context, SourceLine line, string directive)
    {
        var match = QuotedTextRegex().Match(line.Content[directive.Length..].Trim());
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.UnknownLayoutDirective, $"Invalid template {directive} '{line.Content}' - expected '{directive} \"<text>\"'", line.Location);
            return null;
        }

        return StringLiteral.Unescape(match.Groups[1].Value);
    }

    static void ParseContent(ParserContext context, SourceLine line, TemplateMetadata metadata)
    {
        var match = ContentRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.UnknownLayoutDirective, $"Invalid template content '{line.Content}' - expected 'content <slot>'", line.Location);
            context.SkipBlock(line.Indent);
            return;
        }

        metadata.Content.Add(new(match.Groups[1].Value, ScreenParser.ParseDirectiveBlock(context, line), line.Location));
    }

    static void ValidateContentSlots(ParserContext context, string keyword, string name, IReadOnlyList<SlotSyntax> slots, IEnumerable<TemplateSlotContentSyntax> content)
    {
        foreach (var slotContent in content.Where(slotContent => !slots.Any(slot => slot.Name == slotContent.Slot)))
        {
            context.Error(
                DiagnosticCodes.UnknownTemplateContentSlot,
                $"The {keyword} '{name}' provides content for slot '{slotContent.Slot}', which it does not declare",
                slotContent.Location);
        }
    }

    [GeneratedRegex("^\"(" + StringLiteral.BodyPattern + ")\"$", RegexOptions.None, 1000)]
    private static partial Regex QuotedTextRegex();

    [GeneratedRegex(@"^content\s+([a-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex ContentRegex();

    /// <summary>
    /// Collects the template metadata lines while a body is parsed.
    /// </summary>
    sealed class TemplateMetadata
    {
        public List<string>? Scopes { get; set; }

        public string? DisplayName { get; set; }

        public string? Description { get; set; }

        public List<TemplateSlotContentSyntax> Content { get; } = [];
    }
}
