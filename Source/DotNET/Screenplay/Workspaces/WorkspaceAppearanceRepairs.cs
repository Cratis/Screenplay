// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Repairs the deprecated <c>accent</c>, <c>corners</c> and <c>font</c> lines of a ui profile's <c>appearance</c> block by moving
/// every one of them into the theme the profile uses: the profile's own <c>theme</c>, or the design system's default theme.
/// </summary>
/// <remarks>
/// One repair covers the whole block, offered on each of the block's deprecated lines but matched on the first, so a subject and
/// a code select exactly one repair.
/// </remarks>
internal static class WorkspaceAppearanceRepairs
{
    const string AccentEntry = "accent";
    const string RadiusToken = "radius.control";
    const string FontToken = "font.sans";

    /// <summary>
    /// Determines whether a diagnostic location is one of the deprecated lines of an appearance block.
    /// </summary>
    /// <param name="appearance">The <see cref="ProfileAppearanceSyntax"/>.</param>
    /// <param name="location">The diagnostic location.</param>
    /// <returns><see langword="true"/> when the location is the accent, corners or font line.</returns>
    internal static bool IsDeprecatedLine(ProfileAppearanceSyntax appearance, SourceLocation location) =>
        (appearance.Accent is not null && appearance.AccentLocation == location) ||
        (appearance.Corners is not null && appearance.CornersLocation == location) ||
        (appearance.Font is not null && appearance.FontLocation == location);

    /// <summary>
    /// Determines whether a diagnostic location is the first deprecated line of an appearance block, the one the repair is matched on.
    /// </summary>
    /// <param name="appearance">The <see cref="ProfileAppearanceSyntax"/>.</param>
    /// <param name="location">The diagnostic location.</param>
    /// <returns><see langword="true"/> when the location is the first deprecated line.</returns>
    internal static bool IsFirstDeprecatedLine(ProfileAppearanceSyntax appearance, SourceLocation location)
    {
        SourceLocation? first = appearance.Accent is not null ? appearance.AccentLocation : appearance.Corners is not null ? appearance.CornersLocation : appearance.Font is not null ? appearance.FontLocation : null;
        return first == location;
    }

    internal static ImmutableArray<WorkspaceDiagnosticRepair> Find(WorkspaceSyntaxIndex index, WorkspaceRevision revision, Diagnostic diagnostic, bool verify)
    {
        var appearances = index.Entries.Where(entry => entry.Handle.Revision == revision && entry.Node is ProfileAppearanceSyntax appearance && IsDeprecatedLine(appearance, diagnostic.Location)).ToArray();
        if (appearances.Length != 1)
        {
            return [];
        }

        var subject = appearances[0];
        var appearanceNode = (ProfileAppearanceSyntax)subject.Node;
        var profiles = index.Entries.Where(entry => entry.Handle.Revision == revision && entry.Node is UiProfileSyntax profile && ReferenceEquals(profile.Appearance, appearanceNode)).ToArray();
        var designSystems = index.Entries.Where(entry => entry.Handle.Revision == revision && entry.Node is DesignSystemSyntax).ToArray();
        if (profiles.Length != 1 || designSystems.Length != 1)
        {
            return [];
        }

        var profile = (UiProfileSyntax)profiles[0].Node;
        var designSystem = (DesignSystemSyntax)designSystems[0].Node;
        var themeName = profile.Theme ?? designSystem.DefaultTheme;
        var themes = index.Entries.Where(entry => entry.Handle.Revision == revision && entry.Node is ThemeSyntax theme && theme.Name == themeName && theme.Implements == designSystem.Name).ToArray();
        if (themes.Length != 1)
        {
            return [];
        }

        var theme = (ThemeSyntax)themes[0].Node;
        if (!TryMove(appearanceNode, designSystem, theme, out var updatedTheme))
        {
            return [];
        }

        var updatedAppearance = appearanceNode with { Accent = null, AccentLocation = null, Corners = null, CornersLocation = null, Font = null, FontLocation = null };
        var operations = ImmutableArray.CreateBuilder<WorkspaceAstOperation>();
        operations.Add(new ReplaceWorkspaceNode(profiles[0].Handle, profile, profile with { Appearance = updatedAppearance.ColorScheme is null ? null : updatedAppearance }));
        if (!ReferenceEquals(updatedTheme, theme))
        {
            operations.Add(new ReplaceWorkspaceNode(themes[0].Handle, theme, updatedTheme));
        }

        var repair = new WorkspaceDiagnosticRepair(diagnostic.Code, subject.Handle, operations.ToImmutable())
        {
            Title = $"Move the appearance values into theme '{theme.Name}'",
            CanFixAll = false
        };
        return WorkspaceRepairVerification.Discover(index, repair, verify);
    }

    static bool TryMove(ProfileAppearanceSyntax appearance, DesignSystemSyntax designSystem, ThemeSyntax theme, out ThemeSyntax updated)
    {
        updated = theme;
        if (appearance.Accent is { } accent)
        {
            var existing = theme.Palette.FirstOrDefault(entry => entry.Name == AccentEntry);
            if (existing is null)
            {
                updated = updated with { Palette = [.. updated.Palette, new DesignAssignmentSyntax(AccentEntry, accent, true, theme.Location)] };
            }
            else if (!existing.IsLiteral || !string.Equals(existing.Value, accent, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        if (appearance.Corners is { } corners && !TryMoveValue(RadiusToken, CornerRadius(corners), designSystem, ref updated))
        {
            return false;
        }

        return appearance.Font is not { } font || TryMoveValue(FontToken, font, designSystem, ref updated);
    }

    static bool TryMoveValue(string token, string? value, DesignSystemSyntax designSystem, ref ThemeSyntax theme)
    {
        if (value is null || !designSystem.Tokens.Any(category => category.Tokens.Any(candidate => $"{category.Category}.{candidate.Name}" == token)))
        {
            return false;
        }

        // A theme that already sets the token wins: the deprecated line was stale, and removing it loses nothing the theme does not say.
        if (!theme.Values.Any(entry => entry.Name == token))
        {
            theme = theme with { Values = [.. theme.Values, new DesignAssignmentSyntax(token, value, true, theme.Location)] };
        }

        return true;
    }

    static string? CornerRadius(string corners) => corners switch
    {
        "sharp" => "2px",
        "default" => "6px",
        "round" => "12px",
        _ => null
    };
}
