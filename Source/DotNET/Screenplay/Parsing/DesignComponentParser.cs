// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Design;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses the <c>components</c> block of a design system.
/// </summary>
internal static partial class DesignComponentParser
{
    /// <summary>
    /// Parses a <c>components</c> block from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding <c>components</c>.</param>
    /// <param name="components">The components to add to.</param>
    public static void Parse(ParserContext context, SourceLine header, List<DesignComponentSyntax> components)
    {
        if (header.Content != "components")
        {
            context.Error(DiagnosticCodes.InvalidDesignSystemDeclaration, $"Invalid components line '{header.Content}' - expected 'components' followed by an indented block", header.Location);
        }

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (!NameRegex().IsMatch(line.Content))
            {
                context.Error(DiagnosticCodes.InvalidDesignSystemDeclaration, $"Invalid component '{line.Content}' - expected '<Name>' followed by its parts and variants", line.Location);
                context.SkipBlock(line.Indent);
                continue;
            }

            if (components.Exists(existing => existing.Name == line.Content))
            {
                context.Error(DiagnosticCodes.DuplicateDesignToken, $"The component '{line.Content}' is already declared", line.Location);
                context.SkipBlock(line.Indent);
                continue;
            }

            components.Add(ParseComponent(context, line));
        }
    }

    static DesignComponentSyntax ParseComponent(ParserContext context, SourceLine header)
    {
        List<string> parts = [];
        List<DesignVariantSyntax> variants = [];
        var hasParts = false;
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            var word = LineText.FirstWord(line.Content);
            if (word == "parts")
            {
                context.SkipBlock(line.Indent);
                var match = PartsRegex().Match(line.Content);
                if (hasParts || !match.Success)
                {
                    context.Error(DiagnosticCodes.InvalidDesignSystemDeclaration, hasParts ? $"Component '{header.Content}' already declares its parts" : $"Invalid parts line '{line.Content}' - expected 'parts <part>, <part>'", line.Location);
                    continue;
                }

                hasParts = true;
                parts.AddRange(match.Groups[1].Value.Split(',', StringSplitOptions.TrimEntries));
            }
            else if (word == "variant")
            {
                var variant = ParseVariant(context, line);
                if (variant is null)
                {
                    continue;
                }

                if (variants.Exists(existing => existing.Name == variant.Name))
                {
                    context.Error(DiagnosticCodes.DuplicateDesignToken, $"The variant '{variant.Name}' of '{header.Content}' is already declared", line.Location);
                    continue;
                }

                variants.Add(variant);
            }
            else
            {
                context.Error(DiagnosticCodes.InvalidDesignSystemDeclaration, $"Unexpected '{word}' in component '{header.Content}' - expected parts or variant", line.Location);
                context.SkipBlock(line.Indent);
            }
        }

        return new(header.Content, parts, variants, header.Location);
    }

    static DesignVariantSyntax? ParseVariant(ParserContext context, SourceLine header)
    {
        var match = VariantRegex().Match(header.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidDesignSystemDeclaration, $"Invalid variant '{header.Content}' - expected 'variant <name>'", header.Location);
            context.SkipBlock(header.Indent);
            return null;
        }

        List<DesignAssignmentSyntax> parts = [];
        List<DesignStateSyntax> states = [];
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (LineText.FirstWord(line.Content) == "state")
            {
                var state = ParseState(context, line);
                if (state is not null)
                {
                    states.Add(state);
                }

                continue;
            }

            context.SkipBlock(line.Indent);
            AddPart(context, line, parts, $"variant '{match.Groups[1].Value}'");
        }

        return new(match.Groups[1].Value, parts, states, header.Location);
    }

    static DesignStateSyntax? ParseState(ParserContext context, SourceLine header)
    {
        var match = StateRegex().Match(header.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidDesignSystemDeclaration, $"Invalid state '{header.Content}' - expected 'state <name>'", header.Location);
            context.SkipBlock(header.Indent);
            return null;
        }

        List<DesignAssignmentSyntax> parts = [];
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            context.SkipBlock(line.Indent);
            AddPart(context, line, parts, $"state '{match.Groups[1].Value}'");
        }

        return new(match.Groups[1].Value, parts, header.Location);
    }

    static void AddPart(ParserContext context, SourceLine line, List<DesignAssignmentSyntax> parts, string owner)
    {
        if (!DesignAssignmentParser.TryParse(line, out var assignment) || assignment.IsLiteral)
        {
            context.Error(DiagnosticCodes.InvalidDesignSystemDeclaration, $"Invalid part line '{line.Content}' in {owner} - expected '<part> = <category>.<token>'", line.Location);
            return;
        }

        if (parts.Exists(existing => existing.Name == assignment.Name))
        {
            context.Error(DiagnosticCodes.DuplicateDesignToken, $"The part '{assignment.Name}' is already assigned in {owner}", line.Location);
            return;
        }

        parts.Add(assignment);
    }

    [GeneratedRegex(@"^[A-Za-z_]\w*$", RegexOptions.NonBacktracking)]
    private static partial Regex NameRegex();

    [GeneratedRegex(@"^parts\s+([A-Za-z_]\w*(?:\s*,\s*[A-Za-z_]\w*)*)$", RegexOptions.NonBacktracking)]
    private static partial Regex PartsRegex();

    [GeneratedRegex(@"^variant\s+([A-Za-z_][\w\-]*)$", RegexOptions.NonBacktracking)]
    private static partial Regex VariantRegex();

    [GeneratedRegex(@"^state\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex StateRegex();
}
