// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Design;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses a <c>design system</c> block.
/// </summary>
internal static partial class DesignSystemParser
{
    static readonly HashSet<string> _sections = new(StringComparer.Ordinal) { "modes", "default", "tokens", "components", "words", "rules" };

    /// <summary>
    /// Parses a design system from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding the <c>design system</c> header.</param>
    /// <returns>The parsed <see cref="DesignSystemSyntax"/>.</returns>
    public static DesignSystemSyntax Parse(ParserContext context, SourceLine header)
    {
        var match = HeaderRegex().Match(header.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidDesignSystemDeclaration, $"Invalid design system declaration '{header.Content}' - expected 'design system <Name>'", header.Location);
            context.SkipBlock(header.Indent);
            return new(LineText.FirstWord(header.Content), [], [], [], header.Location);
        }

        List<string> modes = [];
        List<DesignTokenCategorySyntax> tokens = [];
        List<DesignComponentSyntax> components = [];
        List<DesignContrastRuleSyntax> rules = [];
        string? defaultTheme = null;
        DesignWordsSyntax? words = null;
        var seen = new HashSet<string>();

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            var word = LineText.FirstWord(line.Content);
            if (_sections.Contains(word) && !seen.Add(word))
            {
                context.Error(DiagnosticCodes.InvalidDesignSystemDeclaration, $"The design system already declares '{word}' - declare it once", line.Location);
                context.SkipBlock(line.Indent);
                continue;
            }

            switch (word)
            {
                case "modes":
                    ParseModes(context, line, modes);
                    break;
                case "default":
                    defaultTheme = ParseDefaultTheme(context, line);
                    break;
                case "tokens":
                    DesignTokenParser.Parse(context, line, tokens);
                    break;
                case "components":
                    DesignComponentParser.Parse(context, line, components);
                    break;
                case "words":
                    words = ParseWords(context, line);
                    break;
                case "rules":
                    ParseRules(context, line, rules);
                    break;
                default:
                    context.Error(DiagnosticCodes.InvalidDesignSystemDeclaration, $"Unexpected '{word}' in design system body - expected modes, default theme, tokens, components, words or rules", line.Location);
                    context.SkipBlock(line.Indent);
                    break;
            }
        }

        return new(match.Groups[1].Value, modes, tokens, components, header.Location)
        {
            DefaultTheme = defaultTheme,
            Words = words,
            Rules = rules
        };
    }

    static void ParseModes(ParserContext context, SourceLine line, List<string> modes)
    {
        context.SkipBlock(line.Indent);
        var match = ModesRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidDesignSystemDeclaration, $"Invalid modes line '{line.Content}' - expected 'modes <name>, <name>'", line.Location);
            return;
        }

        foreach (var mode in match.Groups[1].Value.Split(',', StringSplitOptions.TrimEntries))
        {
            if (modes.Contains(mode))
            {
                context.Error(DiagnosticCodes.DuplicateDesignToken, $"The design system already declares the mode '{mode}'", line.Location);
                continue;
            }

            modes.Add(mode);
        }
    }

    static string? ParseDefaultTheme(ParserContext context, SourceLine line)
    {
        context.SkipBlock(line.Indent);
        var match = DefaultThemeRegex().Match(line.Content);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        context.Error(DiagnosticCodes.InvalidDesignSystemDeclaration, $"Invalid default theme line '{line.Content}' - expected 'default theme <Name>'", line.Location);
        return null;
    }

    static DesignWordsSyntax ParseWords(ParserContext context, SourceLine header)
    {
        List<DesignAssignmentSyntax> voice = [];
        List<DesignAssignmentSyntax> naming = [];
        var seen = new HashSet<string>();
        while (context.TryPeekChild(header.Indent, out var section))
        {
            context.Reader.TakeSignificant();
            var target = section.Content switch
            {
                "voice" => voice,
                "naming" => naming,
                _ => null
            };
            if (target is null || !seen.Add(section.Content))
            {
                context.Error(DiagnosticCodes.InvalidDesignSystemDeclaration, $"Unexpected '{section.Content}' in words - expected 'voice' and 'naming', each once", section.Location);
                context.SkipBlock(section.Indent);
                continue;
            }

            while (context.TryPeekChild(section.Indent, out var entry))
            {
                context.Reader.TakeSignificant();
                if (!DesignAssignmentParser.TryParse(entry, out var assignment) || !assignment.IsLiteral)
                {
                    context.Error(DiagnosticCodes.InvalidDesignSystemDeclaration, $"Invalid {section.Content} entry '{entry.Content}' - expected '<key> = \"<text>\"'", entry.Location);
                    continue;
                }

                if (target.Exists(existing => existing.Name == assignment.Name))
                {
                    context.Error(DiagnosticCodes.DuplicateDesignToken, $"The {section.Content} entry '{assignment.Name}' is already declared", entry.Location);
                    continue;
                }

                target.Add(assignment);
            }
        }

        return new(voice, naming, header.Location);
    }

    static void ParseRules(ParserContext context, SourceLine header, List<DesignContrastRuleSyntax> rules)
    {
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            context.SkipBlock(line.Indent);
            var match = ContrastRegex().Match(line.Content);
            if (!match.Success)
            {
                context.Error(DiagnosticCodes.InvalidDesignSystemDeclaration, $"Invalid rule '{line.Content}' - expected 'contrast <part> on <part> at least <ratio>'", line.Location);
                continue;
            }

            rules.Add(new(match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value, line.Location));
        }
    }

    [GeneratedRegex(@"^design\s+system\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^modes\s+([A-Za-z_][\w\-]*(?:\s*,\s*[A-Za-z_][\w\-]*)*)$", RegexOptions.NonBacktracking)]
    private static partial Regex ModesRegex();

    [GeneratedRegex(@"^default\s+theme\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex DefaultThemeRegex();

    [GeneratedRegex(@"^contrast\s+([A-Za-z_]\w*)\s+on\s+([A-Za-z_]\w*)\s+at\s+least\s+(\d+(?:\.\d+)?)$", RegexOptions.NonBacktracking)]
    private static partial Regex ContrastRegex();
}
