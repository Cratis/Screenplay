// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Design;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Mcp;

/// <summary>
/// Describes an application's design system, its themes and each theme's resolved values per mode for the <c>ui-profiles</c> read tool.
/// </summary>
static class McpDesignSystem
{
    static readonly HashSet<string> _codes =
    [
        DiagnosticCodes.InvalidDesignSystemDeclaration,
        DiagnosticCodes.DuplicateDesignSystem,
        DiagnosticCodes.UnknownDesignMode,
        DiagnosticCodes.DuplicateDesignToken,
        DiagnosticCodes.ThemeValueForUndeclaredToken,
        DiagnosticCodes.ColorTokenMissingModeValue,
        DiagnosticCodes.UnresolvedDesignReference,
        DiagnosticCodes.DesignReferenceCycle,
        DiagnosticCodes.DesignPartCategoryMismatch,
        DiagnosticCodes.ThemeImplementsUnknownDesignSystem,
        DiagnosticCodes.ProfileThemeDoesNotImplementDesignSystem,
        DiagnosticCodes.InvalidDefaultTheme,
        DiagnosticCodes.ContrastBelowRule,
        DiagnosticCodes.DeprecatedAppearanceMember,
        DiagnosticCodes.InvalidThemeBlock
    ];

    /// <summary>
    /// Describes the design system, themes and the theme and mode each profile resolves to.
    /// </summary>
    /// <param name="application">The compiled <see cref="ApplicationSyntax"/>, or <see langword="null"/> when it did not compile.</param>
    /// <param name="diagnostics">The compilation diagnostics.</param>
    /// <returns>An object to serialize.</returns>
    internal static object Describe(ApplicationSyntax? application, IEnumerable<Diagnostic> diagnostics)
    {
        var designSystem = application?.DesignSystems.FirstOrDefault();
        return new
        {
            designSystem = designSystem is null ? null : Describe(designSystem),
            themes = (application?.Themes ?? []).Select(theme => Describe(application!, designSystem, theme)).ToArray(),
            diagnostics = diagnostics.Where(diagnostic => _codes.Contains(diagnostic.Code))
                .Select(diagnostic => new { diagnostic.Code, severity = diagnostic.Severity.ToString(), diagnostic.Message, line = diagnostic.Location.Line, path = diagnostic.Location.Path })
                .ToArray(),
            vocabulary = new
            {
                categories = DesignVocabulary.Categories,
                parts = DesignVocabulary.Parts,
                states = DesignVocabulary.States
            },
            basis = "One design system per application; themes implement it by assigning token values; a ui profile without a theme line uses the design system's default theme. Resolved values are per theme and mode."
        };
    }

    /// <summary>
    /// Describes the theme and mode one profile resolves to.
    /// </summary>
    /// <param name="application">The compiled <see cref="ApplicationSyntax"/>.</param>
    /// <param name="profile">The <see cref="UiProfileSyntax"/>.</param>
    /// <returns>An object to serialize, or <see langword="null"/> when the application has no design system.</returns>
    internal static object? ForProfile(ApplicationSyntax? application, UiProfileSyntax profile)
    {
        if (application?.DesignSystems.Any() != true)
        {
            return null;
        }

        var theme = DesignSystemResolver.SelectTheme(application, profile);
        return new
        {
            theme = theme?.Name,
            selectedByProfile = profile.Theme is not null,
            mode = DesignSystemResolver.SelectMode(application, profile)
        };
    }

    static object Describe(DesignSystemSyntax designSystem) => new
    {
        designSystem.Name,
        modes = designSystem.Modes.ToArray(),
        designSystem.DefaultTheme,
        tokens = designSystem.Tokens.Select(category => new
        {
            category.Category,
            tokens = category.Tokens.Select(token => new { token.Name, token.Description }).ToArray()
        }).ToArray(),
        components = designSystem.Components.Select(component => new
        {
            component.Name,
            parts = component.Parts.ToArray(),
            variants = component.Variants.Select(variant => new
            {
                variant.Name,
                parts = variant.Parts.Select(Assignment).ToArray(),
                states = variant.States.Select(state => new { state.Name, parts = state.Parts.Select(Assignment).ToArray() }).ToArray()
            }).ToArray()
        }).ToArray(),
        words = designSystem.Words is { } words
            ? new { voice = words.Voice.Select(Assignment).ToArray(), naming = words.Naming.Select(Assignment).ToArray() }
            : null,
        rules = designSystem.Rules.Select(rule => new { rule.Foreground, rule.Background, rule.Minimum }).ToArray(),
        line = designSystem.Location.Line,
        path = designSystem.Location.Path
    };

    static object Describe(ApplicationSyntax application, DesignSystemSyntax? designSystem, ThemeSyntax theme)
    {
        var modes = designSystem is not null && theme.Implements == designSystem.Name ? designSystem.Modes.Cast<string?>().DefaultIfEmpty(null).ToArray() : [];
        return new
        {
            theme.Name,
            theme.Implements,
            compatibleWith = theme.CompatibleWith.ToArray(),
            isDefault = designSystem?.DefaultTheme == theme.Name,
            palette = theme.Palette.Select(Assignment).ToArray(),
            values = theme.Values.Select(Assignment).ToArray(),
            modeValues = theme.ModeValues.Select(block => new { block.Mode, values = block.Values.Select(Assignment).ToArray() }).ToArray(),
            resolved = modes.Select(mode => DesignSystemResolver.Resolve(application, theme, mode)).OfType<ResolvedDesignTheme>().Select(resolved => new
            {
                resolved.Mode,
                tokens = resolved.Tokens,
                components = resolved.Components,
                problems = resolved.Problems
            }).ToArray(),
            line = theme.Location.Line,
            path = theme.Location.Path
        };
    }

    static object Assignment(DesignAssignmentSyntax assignment) => new { assignment.Name, assignment.Value, assignment.IsLiteral };
}
