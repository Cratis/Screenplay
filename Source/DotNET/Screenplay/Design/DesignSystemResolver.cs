// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Design;

/// <summary>
/// Resolves a theme of an application's design system into a flat token map and component parts, so hosts do not
/// reimplement reference resolution.
/// </summary>
public static class DesignSystemResolver
{
    /// <summary>
    /// Selects the theme a <c>ui profile</c> applies: the profile's own <c>theme</c>, else the design system's <c>default theme</c>.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/>.</param>
    /// <param name="profile">The <see cref="UiProfileSyntax"/>, or <see langword="null"/> for the application's default.</param>
    /// <returns>The <see cref="ThemeSyntax"/>, or <see langword="null"/> when none is declared.</returns>
    public static ThemeSyntax? SelectTheme(ApplicationSyntax application, UiProfileSyntax? profile)
    {
        var name = profile?.Theme ?? application.DesignSystems.FirstOrDefault()?.DefaultTheme;
        return name is null ? null : (application.Themes ?? []).FirstOrDefault(theme => theme.Name == name);
    }

    /// <summary>
    /// Selects the mode a <c>ui profile</c> asks for: its <c>color scheme</c> when that names a declared mode, else the first declared mode.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/>.</param>
    /// <param name="profile">The <see cref="UiProfileSyntax"/>, or <see langword="null"/>.</param>
    /// <returns>The mode, or <see langword="null"/> when the design system declares none.</returns>
    public static string? SelectMode(ApplicationSyntax application, UiProfileSyntax? profile)
    {
        var modes = application.DesignSystems.FirstOrDefault()?.Modes.ToList() ?? [];
        var scheme = profile?.Appearance?.ColorScheme;
        return scheme is not null && modes.Contains(scheme) ? scheme : modes.FirstOrDefault();
    }

    /// <summary>
    /// Resolves a theme in a mode.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> declaring the design system.</param>
    /// <param name="theme">The <see cref="ThemeSyntax"/> to resolve.</param>
    /// <param name="mode">The mode, or <see langword="null"/> when the design system declares none.</param>
    /// <returns>The <see cref="ResolvedDesignTheme"/>, or <see langword="null"/> when the application has no design system, the theme does not implement it, or the mode is not declared.</returns>
    public static ResolvedDesignTheme? Resolve(ApplicationSyntax application, ThemeSyntax theme, string? mode)
    {
        var designSystem = application.DesignSystems.FirstOrDefault();
        if (designSystem is null || theme.Implements != designSystem.Name || (mode is not null && !designSystem.Modes.Contains(mode)))
        {
            return null;
        }

        var evaluation = new DesignThemeEvaluation(designSystem, theme, mode);
        var components = evaluation.Evaluate();
        return new(designSystem.Name, theme.Name, mode, evaluation.Tokens, components, [.. evaluation.Problems.Select(problem => problem.Message)]);
    }

    /// <summary>
    /// Resolves the theme and mode a <c>ui profile</c> selects.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> declaring the design system.</param>
    /// <param name="profile">The <see cref="UiProfileSyntax"/>, or <see langword="null"/> for the application's default theme.</param>
    /// <returns>The <see cref="ResolvedDesignTheme"/>, or <see langword="null"/> when nothing resolves.</returns>
    public static ResolvedDesignTheme? Resolve(ApplicationSyntax application, UiProfileSyntax? profile) =>
        SelectTheme(application, profile) is { } theme ? Resolve(application, theme, SelectMode(application, profile)) : null;
}
