// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Design;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Validates the design system of an application, the themes that implement it, and the profiles that select them.
/// </summary>
internal static class DesignSystemValidator
{
    /// <summary>
    /// Validates the design system, its themes and the profiles' theme selections.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to validate.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report to.</param>
    public static void Validate(ApplicationSyntax application, ParserContext context)
    {
        var designSystem = application.DesignSystems.FirstOrDefault();
        var themes = (application.Themes ?? []).ToList();
        foreach (var theme in themes.Where(theme => theme.Implements is not null && (designSystem is null || designSystem.Name != theme.Implements)))
        {
            var message = designSystem is null
                ? $"Theme '{theme.Name}' implements '{theme.Implements}', but the application declares no design system"
                : $"Theme '{theme.Name}' implements '{theme.Implements}', but the application's design system is '{designSystem.Name}'";
            context.Error(DiagnosticCodes.ThemeImplementsUnknownDesignSystem, message, theme.Location);
        }

        ValidateAppearance(application, context);
        if (designSystem is null)
        {
            return;
        }

        ValidateDesignSystem(designSystem, context);
        ValidateDefaultTheme(designSystem, themes, context);
        foreach (var theme in themes.Where(theme => theme.Implements == designSystem.Name))
        {
            ValidateTheme(application, designSystem, theme, context);
        }

        foreach (var profile in application.UiProfiles ?? [])
        {
            if (profile.Theme is { } name && themes.Find(theme => theme.Name == name) is { } selected && selected.Implements != designSystem.Name)
            {
                context.Error(DiagnosticCodes.ProfileThemeDoesNotImplementDesignSystem, $"The ui profile '{profile.Name}' selects theme '{name}', which does not implement the design system '{designSystem.Name}'", profile.Location);
            }
        }
    }

    static void ValidateAppearance(ApplicationSyntax application, ParserContext context)
    {
        foreach (var profile in application.UiProfiles ?? [])
        {
            if (profile.Appearance is not { } appearance)
            {
                continue;
            }

            Deprecated(context, appearance.AccentLocation, "accent", "set a palette entry or color token in the selected theme");
            Deprecated(context, appearance.CornersLocation, "corners", "set the radius token values in the selected theme");
            Deprecated(context, appearance.FontLocation, "font", "set the font token values in the selected theme");
        }
    }

    static void Deprecated(ParserContext context, SourceLocation? location, string member, string instead)
    {
        if (location is { } at)
        {
            context.Warning(DiagnosticCodes.DeprecatedAppearanceMember, $"'{member}' in a ui profile's appearance is deprecated - {instead}; appearance keeps only 'color scheme'", at);
        }
    }

    static void ValidateDesignSystem(DesignSystemSyntax designSystem, ParserContext context)
    {
        var declared = designSystem.Tokens.SelectMany(category => category.Tokens.Select(token => (Address: $"{category.Category}.{token.Name}", category.Category))).ToDictionary(token => token.Address, token => token.Category, StringComparer.Ordinal);
        foreach (var component in designSystem.Components)
        {
            ValidateComponent(component, declared, context);
        }

        foreach (var rule in designSystem.Rules)
        {
            foreach (var part in new[] { rule.Foreground, rule.Background }.Where(part => !DesignVocabulary.Parts.Contains(part)))
            {
                context.Error(DiagnosticCodes.InvalidDesignSystemDeclaration, $"Unknown part '{part}' in a contrast rule - expected one of {string.Join(", ", DesignVocabulary.Parts)}", rule.Location);
            }
        }
    }

    static void ValidateComponent(DesignComponentSyntax component, Dictionary<string, string> declared, ParserContext context)
    {
        foreach (var part in component.Parts.Where(part => !DesignVocabulary.Parts.Contains(part)))
        {
            context.Error(DiagnosticCodes.InvalidDesignSystemDeclaration, $"Unknown part '{part}' on component '{component.Name}' - expected one of {string.Join(", ", DesignVocabulary.Parts)}", component.Location);
        }

        foreach (var variant in component.Variants)
        {
            ValidateParts(component, variant.Parts, declared, context);
            foreach (var state in variant.States)
            {
                if (!DesignVocabulary.States.Contains(state.Name))
                {
                    context.Error(DiagnosticCodes.InvalidDesignSystemDeclaration, $"Unknown state '{state.Name}' on '{component.Name}' variant '{variant.Name}' - expected one of {string.Join(", ", DesignVocabulary.States)}", state.Location);
                }

                ValidateParts(component, state.Parts, declared, context);
            }
        }
    }

    static void ValidateParts(DesignComponentSyntax component, IEnumerable<DesignAssignmentSyntax> parts, Dictionary<string, string> declared, ParserContext context)
    {
        foreach (var part in parts)
        {
            if (!component.Parts.Contains(part.Name))
            {
                context.Error(DiagnosticCodes.InvalidDesignSystemDeclaration, $"Component '{component.Name}' does not declare the part '{part.Name}'", part.Location);
                continue;
            }

            if (!declared.TryGetValue(part.Value, out var category))
            {
                context.Error(DiagnosticCodes.UnresolvedDesignReference, $"'{part.Name} = {part.Value}' on '{component.Name}' names no token of the design system", part.Location);
                continue;
            }

            var accepted = DesignVocabulary.CategoriesFor(part.Name);
            if (accepted.Count > 0 && !accepted.Contains(category))
            {
                context.Error(DiagnosticCodes.DesignPartCategoryMismatch, $"The part '{part.Name}' of '{component.Name}' takes a {string.Join(" or ", accepted)} token, not '{part.Value}'", part.Location);
            }
        }
    }

    static void ValidateDefaultTheme(DesignSystemSyntax designSystem, List<ThemeSyntax> themes, ParserContext context)
    {
        if (designSystem.DefaultTheme is not { } name)
        {
            context.Error(DiagnosticCodes.InvalidDefaultTheme, $"Design system '{designSystem.Name}' declares no 'default theme' - a ui profile without a theme resolves to it", designSystem.Location);
            return;
        }

        var theme = themes.Find(candidate => candidate.Name == name);
        if (theme is null)
        {
            context.Error(DiagnosticCodes.InvalidDefaultTheme, $"The default theme '{name}' of design system '{designSystem.Name}' is not declared", designSystem.Location);
        }
        else if (theme.Implements != designSystem.Name)
        {
            context.Error(DiagnosticCodes.InvalidDefaultTheme, $"The default theme '{name}' does not implement design system '{designSystem.Name}' - add 'implements {designSystem.Name}'", theme.Location);
        }
    }

    static void ValidateTheme(ApplicationSyntax application, DesignSystemSyntax designSystem, ThemeSyntax theme, ParserContext context)
    {
        var declared = designSystem.Tokens.SelectMany(category => category.Tokens.Select(token => $"{category.Category}.{token.Name}")).ToHashSet(StringComparer.Ordinal);
        foreach (var block in theme.ModeValues.Where(block => !designSystem.Modes.Contains(block.Mode)))
        {
            context.Error(DiagnosticCodes.UnknownDesignMode, $"Theme '{theme.Name}' has a block for the mode '{block.Mode}', which design system '{designSystem.Name}' does not declare (modes: {string.Join(", ", designSystem.Modes)})", block.Location);
        }

        foreach (var assignment in theme.Values.Concat(theme.ModeValues.SelectMany(block => block.Values)).Where(assignment => !declared.Contains(assignment.Name)))
        {
            context.Error(DiagnosticCodes.ThemeValueForUndeclaredToken, $"Theme '{theme.Name}' assigns '{assignment.Name}', which design system '{designSystem.Name}' does not declare", assignment.Location);
        }

        List<string?> modes = designSystem.Modes.Any() ? [.. designSystem.Modes] : [null];
        var problems = new List<DesignProblem>();
        foreach (var mode in modes)
        {
            var evaluation = new DesignThemeEvaluation(designSystem, theme, mode);
            var components = evaluation.Evaluate();
            problems.AddRange(evaluation.Problems.Where(problem => !problems.Exists(known => known.Code == problem.Code && known.Location == problem.Location)));

            foreach (var token in designSystem.Tokens.Where(category => category.Category == "color").SelectMany(category => category.Tokens.Select(token => $"{category.Category}.{token.Name}")).Where(address => !evaluation.HasValue(address)))
            {
                context.Error(DiagnosticCodes.ColorTokenMissingModeValue, $"Theme '{theme.Name}' gives '{token}' no value{(mode is null ? string.Empty : $" in the mode '{mode}'")}", theme.Location);
            }

            ReportContrast(designSystem, theme, mode, components, context);
        }

        foreach (var problem in problems)
        {
            context.Error(problem.Code, problem.Message, problem.Location);
        }
    }

    static void ReportContrast(DesignSystemSyntax designSystem, ThemeSyntax theme, string? mode, IReadOnlyList<ResolvedDesignComponent> components, ParserContext context)
    {
        foreach (var rule in designSystem.Rules)
        {
            if (!double.TryParse(rule.Minimum, CultureInfo.InvariantCulture, out var minimum))
            {
                continue;
            }

            foreach (var component in components)
            {
                foreach (var variant in component.Variants)
                {
                    var states = new List<(string State, IReadOnlyDictionary<string, string> Parts)> { ("default", variant.Parts) };
                    states.AddRange(variant.States.Where(state => state.Key != "disabled").Select(state => (state.Key, state.Value)));
                    foreach (var (state, parts) in states)
                    {
                        if (parts.TryGetValue(rule.Foreground, out var foreground) && parts.TryGetValue(rule.Background, out var background) &&
                            DesignColor.TryContrast(foreground, background, out var ratio) && ratio < minimum)
                        {
                            var where = state == "default" ? $"{component.Name} {variant.Name}" : $"{component.Name} {variant.Name} {state}";
                            context.Warning(
                                DiagnosticCodes.ContrastBelowRule,
                                $"In theme '{theme.Name}'{(mode is null ? string.Empty : $" ({mode})")}, {where}: {rule.Foreground} {foreground} on {rule.Background} {background} has contrast {ratio.ToString("0.##", CultureInfo.InvariantCulture)}, below the required {rule.Minimum}",
                                theme.Location);
                        }
                    }
                }
            }
        }
    }
}
