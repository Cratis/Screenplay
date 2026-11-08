// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Printing;

public partial class ScreenplayPrinter
{
    static void WriteAssignments(ScreenplayWriter writer, IEnumerable<DesignAssignmentSyntax> assignments)
    {
        var list = assignments.ToList();
        var width = list.Count == 0 ? 0 : list.Max(assignment => assignment.Name.Length);
        foreach (var assignment in list)
        {
            writer.Line($"{assignment.Name.PadRight(width)} = {(assignment.IsLiteral ? StringLiteral.Quote(assignment.Value) : assignment.Value)}", assignment);
        }
    }

    static void WriteTheme(ScreenplayWriter writer, ThemeSyntax theme)
    {
        using var anchor = writer.Anchor(theme);
        writer.Line($"theme {theme.Name}");
        using (writer.Indent())
        {
            if (theme.Implements is not null)
            {
                writer.DirectiveLine($"implements {theme.Implements}", theme, "implements");
            }

            var packages = theme.CompatibleWith.ToList();
            for (var index = 0; index < packages.Count; index++)
            {
                writer.DirectiveLine($"compatible with {packages[index]}", theme, DirectiveLocationKeys.ForValue("compatible", packages, index));
            }

            WriteAssignmentSection(writer, "palette", theme.Palette);
            WriteAssignmentSection(writer, "values", theme.Values);
            foreach (var mode in theme.ModeValues)
            {
                writer.Line(mode.Mode, mode);
                using (writer.Indent())
                {
                    WriteAssignments(writer, mode.Values);
                }
            }
        }
    }

    static void WriteAssignmentSection(ScreenplayWriter writer, string heading, IEnumerable<DesignAssignmentSyntax> assignments)
    {
        if (!assignments.Any())
        {
            return;
        }

        writer.Line(heading);
        using (writer.Indent())
        {
            WriteAssignments(writer, assignments);
        }
    }

    static void WriteDesignSystem(ScreenplayWriter writer, DesignSystemSyntax designSystem)
    {
        using var anchor = writer.Anchor(designSystem);
        writer.Line($"design system {designSystem.Name}");
        using (writer.Indent())
        {
            var modes = designSystem.Modes.ToList();
            if (modes.Count > 0)
            {
                writer.Line($"modes {string.Join(", ", modes)}");
            }

            if (designSystem.DefaultTheme is not null)
            {
                writer.Line($"default theme {designSystem.DefaultTheme}");
            }

            WriteTokens(writer, designSystem.Tokens);
            WriteComponents(writer, designSystem.Components);
            WriteWords(writer, designSystem.Words);
            var rules = designSystem.Rules.ToList();
            if (rules.Count > 0)
            {
                writer.Line("rules");
                using (writer.Indent())
                {
                    foreach (var rule in rules)
                    {
                        writer.Line($"contrast {rule.Foreground} on {rule.Background} at least {rule.Minimum}", rule);
                    }
                }
            }
        }
    }

    static void WriteTokens(ScreenplayWriter writer, IEnumerable<DesignTokenCategorySyntax> categories)
    {
        var categoryList = categories.ToList();
        if (categoryList.Count == 0)
        {
            return;
        }

        writer.Line("tokens");
        using (writer.Indent())
        {
            foreach (var category in categoryList)
            {
                writer.Line(category.Category, category);
                using (writer.Indent())
                {
                    var tokens = category.Tokens.ToList();
                    var width = tokens.Exists(token => token.Description is not null) ? tokens.Max(token => token.Name.Length) : 0;
                    foreach (var token in tokens)
                    {
                        writer.Line(token.Description is null ? token.Name : $"{token.Name.PadRight(width)} {StringLiteral.Quote(token.Description)}", token);
                    }
                }
            }
        }
    }

    static void WriteComponents(ScreenplayWriter writer, IEnumerable<DesignComponentSyntax> components)
    {
        var componentList = components.ToList();
        if (componentList.Count == 0)
        {
            return;
        }

        writer.Line("components");
        using (writer.Indent())
        {
            foreach (var component in componentList)
            {
                writer.Line(component.Name, component);
                using (writer.Indent())
                {
                    if (component.Parts.Any())
                    {
                        writer.Line($"parts {string.Join(", ", component.Parts)}");
                    }

                    foreach (var variant in component.Variants)
                    {
                        writer.Line($"variant {variant.Name}", variant);
                        using (writer.Indent())
                        {
                            WriteAssignments(writer, variant.Parts);
                            foreach (var state in variant.States)
                            {
                                writer.Line($"state {state.Name}", state);
                                using (writer.Indent())
                                {
                                    WriteAssignments(writer, state.Parts);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    static void WriteWords(ScreenplayWriter writer, DesignWordsSyntax? words)
    {
        if (words is null)
        {
            return;
        }

        writer.Line("words", words);
        using (writer.Indent())
        {
            foreach (var (heading, entries) in new[] { ("voice", words.Voice), ("naming", words.Naming) })
            {
                if (!entries.Any())
                {
                    continue;
                }

                writer.Line(heading);
                using (writer.Indent())
                {
                    WriteAssignments(writer, entries);
                }
            }
        }
    }
}
