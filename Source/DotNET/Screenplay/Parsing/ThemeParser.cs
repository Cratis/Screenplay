// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>theme</c> declarations - a named visual theme and the component packages it is compatible with.
/// </summary>
internal static partial class ThemeParser
{
    /// <summary>
    /// Parses a theme from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding the <c>theme</c> header.</param>
    /// <returns>The parsed <see cref="ThemeSyntax"/>.</returns>
    public static ThemeSyntax Parse(ParserContext context, SourceLine header)
    {
        var match = HeaderRegex().Match(header.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidThemeDeclaration, $"Invalid theme declaration '{header.Content}' - expected 'theme <Name>'", header.Location);
            context.SkipBlock(header.Indent);
            return new(LineText.FirstWord(header.Content), [], header.Location);
        }

        var name = match.Groups[1].Value;
        var compatibleWith = new List<string>();
        var seen = new HashSet<string>();
        var directiveLocations = new Dictionary<string, SourceLocation>();
        string? implements = null;
        List<DesignAssignmentSyntax> palette = [];
        List<DesignAssignmentSyntax> values = [];
        List<ThemeModeValuesSyntax> modeValues = [];
        var sections = new HashSet<string>();

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            var word = LineText.FirstWord(line.Content);
            if (word == "implements")
            {
                context.SkipBlock(line.Indent);
                var implementsMatch = ImplementsRegex().Match(line.Content);
                if (!implementsMatch.Success || implements is not null)
                {
                    context.Error(DiagnosticCodes.InvalidThemeBlock, implements is not null ? $"Theme '{name}' already declares which design system it implements" : $"Invalid implements line '{line.Content}' - expected 'implements <DesignSystem>'", line.Location);
                    continue;
                }

                implements = implementsMatch.Groups[1].Value;
                directiveLocations["implements"] = line.Location;
                continue;
            }

            if (string.Equals(word, "palette", StringComparison.Ordinal) || string.Equals(word, "values", StringComparison.Ordinal))
            {
                var target = word == "palette" ? palette : values;
                if (line.Content != word || !sections.Add(word))
                {
                    context.Error(DiagnosticCodes.InvalidThemeBlock, line.Content != word ? $"Invalid {word} line '{line.Content}' - expected '{word}' followed by an indented block" : $"Theme '{name}' already declares {word}", line.Location);
                    context.SkipBlock(line.Indent);
                    continue;
                }

                ParseAssignments(context, line, word, target);
                continue;
            }

            var compatibleMatch = CompatibleWithRegex().Match(line.Content);
            if (compatibleMatch.Success)
            {
                var package = compatibleMatch.Groups[1].Value;
                if (!seen.Add(package))
                {
                    context.Error(DiagnosticCodes.DuplicateCompatibleWith, $"Theme '{name}' already declares compatibility with '{package}'", line.Location);
                    continue;
                }

                compatibleWith.Add(package);
                directiveLocations[DirectiveLocationKeys.ForValue("compatible", compatibleWith, compatibleWith.Count - 1)] = line.Location;
                continue;
            }

            if (ModeRegex().IsMatch(line.Content) && context.TryPeekChild(line.Indent, out _))
            {
                if (modeValues.Exists(existing => existing.Mode == line.Content))
                {
                    context.Error(DiagnosticCodes.InvalidThemeBlock, $"Theme '{name}' already declares values for the mode '{line.Content}'", line.Location);
                    context.SkipBlock(line.Indent);
                    continue;
                }

                List<DesignAssignmentSyntax> modeAssignments = [];
                ParseAssignments(context, line, $"mode {line.Content}", modeAssignments);
                modeValues.Add(new(line.Content, modeAssignments, line.Location));
                continue;
            }

            context.Error(DiagnosticCodes.InvalidCompatibleWithDeclaration, $"Invalid theme line '{line.Content}' - expected 'compatible with <Package>', 'implements <DesignSystem>', 'palette', 'values' or a mode block", line.Location);
            context.SkipBlock(line.Indent);
        }

        return new(name, compatibleWith, header.Location)
        {
            DirectiveLocations = directiveLocations,
            Implements = implements,
            Palette = palette,
            Values = values,
            ModeValues = modeValues
        };
    }

    static void ParseAssignments(ParserContext context, SourceLine header, string section, List<DesignAssignmentSyntax> assignments)
    {
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            context.SkipBlock(line.Indent);
            if (!DesignAssignmentParser.TryParse(line, out var assignment))
            {
                context.Error(DiagnosticCodes.InvalidThemeBlock, $"Invalid {section} line '{line.Content}' - expected '<name> = \"<value>\"' or '<name> = <reference>'", line.Location);
                continue;
            }

            if (assignments.Exists(existing => existing.Name == assignment.Name))
            {
                context.Error(DiagnosticCodes.DuplicateDesignToken, $"'{assignment.Name}' is already assigned in {section}", line.Location);
                continue;
            }

            assignments.Add(assignment);
        }
    }

    [GeneratedRegex(@"^theme\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^compatible\s+with\s+([A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)$", RegexOptions.NonBacktracking)]
    private static partial Regex CompatibleWithRegex();

    [GeneratedRegex(@"^implements\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex ImplementsRegex();

    [GeneratedRegex(@"^[A-Za-z_][\w\-]*$", RegexOptions.NonBacktracking)]
    private static partial Regex ModeRegex();
}
