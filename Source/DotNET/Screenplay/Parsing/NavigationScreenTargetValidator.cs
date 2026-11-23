// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Resolves item-local destinations against executable screen ownership, never descendant features.
/// </summary>
internal sealed class NavigationScreenTargets
{
    readonly List<(ScreenSyntax Screen, SliceSyntax Slice, string Feature)> _screens = [];

    internal NavigationScreenTargets(ApplicationSyntax application)
    {
        var slices = ScreenplayValidator.ScopedSlices(application).ToArray();
        var declarations = ScreenplayValidator.ScopedScreens(application).ToArray();
        var claimed = new HashSet<ScreenSyntax>(ReferenceEqualityComparer.Instance);
        foreach (var (slice, scope) in slices)
        {
            foreach (var screen in slice.Screens)
            {
                claimed.Add(screen);
                _screens.Add((screen, slice, FeaturePath(scope)));
            }
        }

        var names = declarations.Select(screen => new Declaration(screen.Screen.Name, screen.Scope)).ToArray();
        foreach (var (slice, scope) in slices)
        {
            foreach (var adoption in slice.UsedScreens)
            {
                var resolved = ReferenceResolver.Resolve(adoption.Screen, scope, names).Resolved;
                if (resolved is null) continue;
                var declaration = declarations.Single(screen => screen.Screen.Name == resolved.Name && screen.Scope == resolved.Scope);
                if (claimed.Add(declaration.Screen)) _screens.Add((declaration.Screen, slice, FeaturePath(scope)));
            }
        }
    }

    internal static bool IsPage(ScreenSyntax screen) => !screen.Fits.Any() || screen.Fits.Contains(ScreenFit.Page);

    internal IReadOnlyList<ScreenSyntax> Resolve(NavigationTargetSyntax target) =>
        _screens.Where(entry => entry.Feature == $"{target.Module}.{target.Feature}" && entry.Screen.Name == target.Screen).Select(entry => entry.Screen).ToArray();

    internal bool SelectsSlice(NavigationTargetSyntax target, string slicePath) =>
        _screens.Exists(entry => entry.Feature == $"{target.Module}.{target.Feature}" && entry.Screen.Name == target.Screen && $"{entry.Feature}.{entry.Slice.Name}" == slicePath);

    internal void Validate(NavigationTargetSyntax target, ParserContext context)
    {
        if (target.Screen is null) return;
        var screens = Resolve(target);
        if (screens.Count == 0) context.Error(DiagnosticCodes.UnknownNavigationScreen, $"Navigation target '{target.Module}.{target.Feature}' has no executable screen '{target.Screen}'", target.Location);
        else if (screens.Count > 1) context.Error(DiagnosticCodes.AmbiguousNavigationScreen, $"Navigation target '{target.Module}.{target.Feature}' has more than one executable screen named '{target.Screen}'", target.Location);
        else if (!IsPage(screens[0])) context.Error(DiagnosticCodes.NonPageNavigationScreen, $"Navigation screen '{target.Screen}' does not fit page", target.Location);
    }

    static string FeaturePath(DeclarationScope scope) => string.Join('.', scope.Segments.Take(scope.Depth - 1));
}
