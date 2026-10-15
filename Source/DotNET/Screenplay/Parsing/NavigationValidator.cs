// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Validates the navigation regions of an application - the ones its layouts declare and the ones its modules
/// inherit, extend or override - against the modules, features and slots the document declares.
/// </summary>
/// <remarks>
/// The rules mirror Scene's navigation diagnostics (<c>navDuplicateId</c>, <c>navTargetMissing</c>,
/// <c>navSlotUnknown</c>, <c>navTargetOutOfScope</c>, <c>navModuleWithoutFeatures</c>) so a model authored here
/// reports the same findings in the designer. Names resolve the way <c>depends on</c> and contributions already
/// do: by the module and feature names the document declares.
/// </remarks>
internal static class NavigationValidator
{
    /// <summary>
    /// Validates every navigation region of the application.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to validate.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    public static void Validate(ApplicationSyntax application, ParserContext context)
    {
        var layouts = application.Layouts ?? [];
        var modules = application.Modules.ToList();
        if (!layouts.Any(layout => layout.Navigation.Any()) && !modules.Exists(module => module.Navigation.Any()))
        {
            return;
        }

        var structure = new Structure(modules);
        var slots = layouts.SelectMany(layout => layout.Slots.Select(slot => slot.Name)).ToHashSet(StringComparer.Ordinal);
        var applicationIds = new Dictionary<NavigationRegionKind, HashSet<string>>();

        foreach (var layout in layouts)
        {
            ValidateLayout(layout, structure, context, applicationIds);
        }

        foreach (var module in modules)
        {
            ValidateModule(module, structure, layouts.Any() ? slots : null, applicationIds, context);
        }
    }

    static void ValidateLayout(LayoutSyntax layout, Structure structure, ParserContext context, Dictionary<NavigationRegionKind, HashSet<string>> applicationIds)
    {
        var layoutSlots = layout.Slots.Select(slot => slot.Name).ToHashSet(StringComparer.Ordinal);
        var seen = new HashSet<NavigationRegionKind>();
        var hasLogo = false;
        foreach (var region in layout.Navigation)
        {
            var name = RegionName(region.Region);
            if (!seen.Add(region.Region))
            {
                context.Error(DiagnosticCodes.DuplicateNavigationRegion, $"The layout '{layout.Name}' already declares 'navigation {name}' - a region is declared once", region.Location);
                continue;
            }

            if (region.Mode != NavigationRegionMode.Unspecified)
            {
                context.Error(DiagnosticCodes.NavigationRegionModeNotAllowed, $"The layout '{layout.Name}' declares 'navigation {name}' with a mode - the application is the base of every region and has none; only a module inherits, extends or overrides", region.Location);
            }

            if (region.Logo is { } regionLogo)
            {
                if (hasLogo)
                {
                    context.Error(DiagnosticCodes.NavigationLogoNotAllowed, $"The layout '{layout.Name}' declares a second logo - the application has one logo", regionLogo.Location);
                }

                hasLogo = true;
            }

            if (!applicationIds.TryGetValue(region.Region, out var ids))
            {
                applicationIds[region.Region] = ids = new(StringComparer.Ordinal);
            }

            ValidateItems(region.Items, region.Region, $"layout '{layout.Name}'", null, structure, layoutSlots, new(StringComparer.Ordinal), ids, [], context);
        }
    }

    static void ValidateModule(ModuleSyntax module, Structure structure, HashSet<string>? slots, Dictionary<NavigationRegionKind, HashSet<string>> applicationIds, ParserContext context)
    {
        var seen = new HashSet<NavigationRegionKind>();
        foreach (var region in module.Navigation)
        {
            var name = RegionName(region.Region);
            if (!seen.Add(region.Region))
            {
                context.Error(DiagnosticCodes.DuplicateNavigationRegion, $"The module '{module.Name}' already declares 'navigation {name}' - a region is declared once", region.Location);
                continue;
            }

            if (region.Logo is { } logo)
            {
                context.Error(DiagnosticCodes.NavigationLogoNotAllowed, $"The module '{module.Name}' declares a logo - the logo is the application's and belongs in the layout", logo.Location);
            }

            if (region.Mode == NavigationRegionMode.Inherit && region.Items.Any())
            {
                context.Error(DiagnosticCodes.NavigationRegionModeNotAllowed, $"The module '{module.Name}' declares 'navigation {name} inherits' with items - an inheriting region shows the application's items and clears its own; use 'extends' or 'overrides'", region.Location);
            }

            if (region.Mode == NavigationRegionMode.Override && !region.Items.Any())
            {
                context.Warning(DiagnosticCodes.NavOverrideWithoutItems, $"The module '{module.Name}' overrides 'navigation {name}' without items - the region is empty in this module", region.Location);
            }

            var inherited = region.Mode == NavigationRegionMode.Extend && applicationIds.TryGetValue(region.Region, out var ids) ? ids : [];
            ValidateItems(region.Items, region.Region, $"module '{module.Name}'", module.Name, structure, slots, new(StringComparer.Ordinal), [], inherited, context);
        }
    }

    static void ValidateItems(
        IEnumerable<NavigationItemSyntax> items,
        NavigationRegionKind region,
        string owner,
        string? ownModule,
        Structure structure,
        HashSet<string>? slots,
        HashSet<string> seenIds,
        HashSet<string> collectIds,
        HashSet<string> inheritedIds,
        ParserContext context,
        bool nested = false)
    {
        var name = RegionName(region);
        foreach (var item in items)
        {
            if (!seenIds.Add(item.Id))
            {
                context.Error(DiagnosticCodes.NavDuplicateId, $"Navigation item '{item.Id}' is used more than once in {owner} navigation {name}", item.Location);
            }
            else if (inheritedIds.Contains(item.Id))
            {
                context.Error(DiagnosticCodes.NavDuplicateId, $"Navigation item '{item.Id}' in {owner} navigation {name} is also used by an application item, and the region extends the application", item.Location);
            }

            collectIds.Add(item.Id);

            if (nested && region == NavigationRegionKind.Topbar)
            {
                context.Error(DiagnosticCodes.NavigationChildrenNotAllowed, $"Navigation item '{item.Id}' is nested in the topbar of {owner} - only the sidebar nests items", item.Location);
            }

            if (item.Slot is not null && slots?.Contains(item.Slot) == false)
            {
                context.Error(DiagnosticCodes.NavSlotUnknown, $"Navigation item '{item.Id}' in {owner} renders into slot '{item.Slot}', which no layout declares", item.Location);
            }

            ValidateTarget(item, owner, ownModule, structure, context);
            ValidateItems(item.Children, region, owner, ownModule, structure, slots, seenIds, collectIds, inheritedIds, context, nested: true);
        }
    }

    static void ValidateTarget(NavigationItemSyntax item, string owner, string? ownModule, Structure structure, ParserContext context)
    {
        if (item.Target is not { } target)
        {
            if (!item.Children.Any())
            {
                context.Error(DiagnosticCodes.NavTargetMissing, $"Navigation item '{item.Id}' in {owner} has no target and no sub-items - add 'target <Module>[.<Feature>]'", item.Location);
            }

            return;
        }

        if (!structure.Modules.TryGetValue(target.Module, out var features))
        {
            context.Error(DiagnosticCodes.NavTargetMissing, $"Navigation item '{item.Id}' in {owner} targets module '{target.Module}', which does not exist", target.Location);
            return;
        }

        if (ownModule is not null && !string.Equals(ownModule, target.Module, StringComparison.Ordinal))
        {
            context.Error(DiagnosticCodes.NavTargetOutOfScope, $"Navigation item '{item.Id}' of module '{ownModule}' targets module '{target.Module}'; a module's own items may only target that module", target.Location);
            return;
        }

        if (target.Feature is not null)
        {
            if (!features.Contains(target.Feature))
            {
                context.Error(DiagnosticCodes.NavTargetMissing, $"Navigation item '{item.Id}' in {owner} targets feature '{target.Module}.{target.Feature}', which does not exist", target.Location);
            }

            return;
        }

        if (features.Count == 0)
        {
            context.Warning(DiagnosticCodes.NavModuleWithoutFeatures, $"Navigation item '{item.Id}' in {owner} targets module '{target.Module}', which has no feature to go to", target.Location);
        }
    }

    static string RegionName(NavigationRegionKind region) => region == NavigationRegionKind.Sidebar ? "sidebar" : "topbar";

    sealed class Structure
    {
        public Structure(IEnumerable<ModuleSyntax> modules)
        {
            foreach (var module in modules)
            {
                if (!Modules.TryGetValue(module.Name, out var paths))
                {
                    Modules[module.Name] = paths = new(StringComparer.Ordinal);
                }

                foreach (var path in module.Features.SelectMany(feature => Paths(feature, string.Empty)))
                {
                    paths.Add(path);
                }
            }
        }

        public Dictionary<string, HashSet<string>> Modules { get; } = new(StringComparer.Ordinal);

        static IEnumerable<string> Paths(FeatureSyntax feature, string prefix)
        {
            var path = prefix.Length == 0 ? feature.Name : $"{prefix}.{feature.Name}";
            yield return path;
            foreach (var nested in feature.Features.SelectMany(child => Paths(child, path)))
            {
                yield return nested;
            }
        }
    }
}
