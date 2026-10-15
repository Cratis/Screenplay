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
    static readonly string[] _sliceKinds = ["StateChange", "StateView"];

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
                if (ParseScopes(context, line) is { } parsed)
                {
                    (metadata.Scopes, metadata.SliceKinds) = parsed;
                }

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

    static (List<string> Scopes, List<string> SliceKinds)? ParseScopes(ParserContext context, SourceLine line)
    {
        var text = line.Content["scopes".Length..].Trim();
        if (text == "none")
        {
            return ([], []);
        }

        var scopes = new List<string>();
        var sliceKinds = new List<string>();
        var valid = text.Length > 0;
        foreach (var item in text.Split(',', StringSplitOptions.TrimEntries))
        {
            var words = item.Split([' ', '|'], StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0 || !_templateScopes.Contains(words[0], StringComparer.Ordinal) || (words.Length > 1 && words[0] != "slice"))
            {
                valid = false;
                continue;
            }

            scopes.Add(words[0]);
            foreach (var word in words.Skip(1))
            {
                var kind = _sliceKinds.FirstOrDefault(candidate => string.Equals(candidate, word, StringComparison.OrdinalIgnoreCase));
                if (kind is null)
                {
                    valid = false;
                }
                else if (!sliceKinds.Contains(kind, StringComparer.Ordinal))
                {
                    sliceKinds.Add(kind);
                }
            }
        }

        if (!valid)
        {
            context.Error(
                DiagnosticCodes.UnknownLayoutDirective,
                $"Invalid template scopes '{line.Content}' - expected 'scopes <application|module|feature|subfeature|slice>, ...' where slice may be followed by 'statechange' and/or 'stateview', or 'scopes none'",
                line.Location);
            return null;
        }

        return (scopes, sliceKinds);
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

        public List<string> SliceKinds { get; set; } = [];

        public string? DisplayName { get; set; }

        public string? Description { get; set; }

        public List<TemplateSlotContentSyntax> Content { get; } = [];
    }
}
