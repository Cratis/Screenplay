// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

internal static partial class ScreenplayValidator
{
    /// <summary>
    /// Validates that the <c>variant</c>s of every <c>freeform</c> arrangement agree on which slots exist -
    /// a variant that omits a slot another variant of the same arrangement places (or explicitly hides)
    /// leaves that slot's presence undefined for the size class the omitting variant targets.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to validate.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <remarks>
    /// Whether a <c>ui profile</c> targeting a size class with no matching <c>variant</c> should warn is a
    /// build-time concern - it depends on which templates a profile's screens actually resolve to, which the
    /// Screenplay compiler does not know. That check belongs to Stage's build pipeline, not here.
    /// </remarks>
    static void ValidateArrangements(ApplicationSyntax application, ParserContext context)
    {
        foreach (var (kind, name, arrangement) in Arrangements(application))
        {
            ValidateVariants(kind, name, arrangement, context);
        }
    }

    static void ValidateVariants(string kind, string name, ArrangementSyntax arrangement, ParserContext context)
    {
        var variants = (arrangement.Variants ?? []).ToList();
        if (variants.Count == 0)
        {
            return;
        }

        var allSlots = variants.SelectMany(variant => variant.Places.Select(place => place.SlotName)).ToHashSet();

        foreach (var variant in variants)
        {
            var declared = variant.Places.Select(place => place.SlotName).ToHashSet();
            foreach (var missing in allSlots.Except(declared))
            {
                context.Warning(
                    DiagnosticCodes.VariantMissingSlot,
                    $"The {kind} '{name}' has a variant for width {variant.Width}, height {variant.Height} that does not mention slot '{missing}' - place it or declare it 'hidden'",
                    variant.Location);
            }
        }
    }

    /// <summary>
    /// Yields every <c>arrangement</c> in the document with what declares it.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to walk.</param>
    /// <returns>The kind and name of each structure that arranges its slots, with its arrangement.</returns>
    static IEnumerable<(string Kind, string Name, ArrangementSyntax Arrangement)> Arrangements(ApplicationSyntax application) =>
        (application.Layouts ?? []).Select(layout => (Kind: "layout", layout.Name, layout.Arrangement))
            .Concat(application.Modules.SelectMany(ModuleArrangements))
            .Where(entry => entry.Arrangement is not null)
            .Select(entry => (entry.Kind, entry.Name, entry.Arrangement!));

    static IEnumerable<(string Kind, string Name, ArrangementSyntax? Arrangement)> ModuleArrangements(ModuleSyntax module) =>
        module.ScreenTemplates.Select(template => (Kind: "screen template", template.Name, template.Arrangement))
            .Concat((module.DialogTemplates ?? []).Select(template => (Kind: "dialog template", template.Name, template.Arrangement)));

    /// <summary>
    /// Validates that every <c>ui profile</c> selecting a layout names one the document declares.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to validate.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <remarks>
    /// Reported the same way an unknown theme is - a warning, because a name may still resolve to something
    /// outside the document, and the point is that the gap stays visible.
    /// </remarks>
    static void ValidateProfileLayouts(ApplicationSyntax application, ParserContext context)
    {
        var layouts = (application.Layouts ?? []).Select(layout => layout.Name).ToHashSet(StringComparer.Ordinal);

        foreach (var profile in application.UiProfiles ?? [])
        {
            if (profile.Layout is { } layoutName && !layouts.Contains(layoutName))
            {
                context.Warning(DiagnosticCodes.UnknownLayout, $"Unknown layout '{layoutName}' - declare it with 'layout {layoutName}'", profile.Location);
            }
        }
    }

    /// <summary>
    /// Validates that every <c>ui profile</c> selecting a theme names one the document declares, and that
    /// the theme is declared compatible with every package the profile itself lists.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to validate.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <remarks>
    /// An arbitrary theme/package pairing can silently produce unstyled or broken components, so a profile
    /// selecting a theme not declared compatible with one of its own packages is reported - the pairing
    /// might still work by coincidence, but the gap is made visible the same way an unknown or ambiguous
    /// name already is.
    /// </remarks>
    static void ValidateThemes(ApplicationSyntax application, ParserContext context)
    {
        var themes = (application.Themes ?? []).ToDictionary(theme => theme.Name, StringComparer.Ordinal);

        foreach (var profile in application.UiProfiles ?? [])
        {
            if (profile.Theme is { } themeName)
            {
                ValidateProfileTheme(profile, themeName, themes, context);
            }
        }
    }

    static void ValidateProfileTheme(UiProfileSyntax profile, string themeName, Dictionary<string, ThemeSyntax> themes, ParserContext context)
    {
        if (!themes.TryGetValue(themeName, out var theme))
        {
            context.Warning(DiagnosticCodes.UnknownTheme, $"Unknown theme '{themeName}' - declare it with 'theme {themeName}'", profile.Location);
            return;
        }

        var compatibleWith = theme.CompatibleWith.ToHashSet(StringComparer.Ordinal);
        foreach (var package in profile.Packages.Where(package => !compatibleWith.Contains(package)))
        {
            context.Warning(
                DiagnosticCodes.ThemeNotCompatibleWithPackage,
                $"Theme '{themeName}' is not declared compatible with package '{package}' - components from that package may not receive {themeName}'s styling",
                profile.Location);
        }
    }

    /// <summary>
    /// Validates that every contribution in the document resolves to a contribution point some slot of the
    /// application's layout or of a module's template declares, and that the screen it navigates to, if any, resolves.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to validate.</param>
    /// <param name="screens">The screens the document declares, scoped to where they are declared.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <remarks>
    /// A contribution resolves to the nearest enclosing structure that declares a matching contribution
    /// point: first the contribution's own module - a module with its own matching slot stops contributions
    /// inside it from bubbling further out - then every other module in the document, and finally the
    /// application's own layouts, which belong to no module and are the outermost shell of all. This is a
    /// different algorithm from <see cref="ReferenceResolver"/>: it walks a physical containment tree
    /// (module owns template owns slot) rather than narrowing by declaration-scope depth.
    /// </remarks>
    static void ValidateContributions(ApplicationSyntax application, IReadOnlyList<Declaration> screens, ParserContext context)
    {
        var offers = application.Modules
            .SelectMany(module => Structures(module).SelectMany(structure => structure.Slots
                .Where(slot => slot.Contributes is not null)
                .Select(slot => (Module: module.Name, Structure: structure.Name, Slot: slot.Name, ContributionPoint: slot.Contributes!))))
            .Concat((application.Layouts ?? []).SelectMany(layout => layout.Slots
                .Where(slot => slot.Contributes is not null)
                .Select(slot => (Module: string.Empty, Structure: layout.Name, Slot: slot.Name, ContributionPoint: slot.Contributes!))))
            .ToList();
        var validator = new ContributionValidator(offers, screens, context);

        foreach (var module in application.Modules)
        {
            var moduleScope = new DeclarationScope([module.Name]);
            foreach (var contribution in module.Contributions ?? [])
            {
                validator.Validate(contribution, module.Name, moduleScope);
            }

            foreach (var (feature, scope) in AllFeaturesScoped(module.Features, [module.Name]))
            {
                foreach (var contribution in feature.Contributions ?? [])
                {
                    validator.Validate(contribution, module.Name, scope);
                }
            }
        }
    }

    /// <summary>
    /// Yields the slot bearing structures a module declares - its screen templates and its dialog templates.
    /// </summary>
    /// <param name="module">The <see cref="ModuleSyntax"/> to read.</param>
    /// <returns>The name and slots of each.</returns>
    static IEnumerable<(string Name, IEnumerable<SlotSyntax> Slots)> Structures(ModuleSyntax module) =>
        module.ScreenTemplates.Select(template => (template.Name, template.Slots))
            .Concat((module.DialogTemplates ?? []).Select(template => (template.Name, template.Slots)));
}
