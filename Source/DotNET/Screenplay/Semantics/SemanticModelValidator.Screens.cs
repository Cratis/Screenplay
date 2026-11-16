// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    static readonly string[] _screenSubjects = ["screen", "item"];

    // ESM v42 constructs: screens as a stack-neutral UI contract.
    internal static bool UsesScreens(SemanticApplication application) =>
        SlicesOf(application).Any(slice => !slice.Screens.IsDefaultOrEmpty);

    // ESM v52 constructs: extension components placed on screens, and the extensions a screen needs because of them.
    internal static bool UsesExtensionComponents(SemanticApplication application) =>
        SlicesOf(application).SelectMany(slice => slice.Screens).Concat(application.Screens).Any(screen => !screen.Components.IsDefaultOrEmpty || !screen.Extensions.IsDefaultOrEmpty);

    // ESM v56 constructs: screens declared free of any slice, the needs a screen leaves to its adopters, and the adoptions.
    internal static bool UsesFreeScreens(SemanticApplication application) =>
        !application.Screens.IsDefaultOrEmpty ||
        SlicesOf(application).Any(slice => !slice.Adoptions.IsDefaultOrEmpty || slice.Screens.Any(screen => !screen.Needs.IsDefaultOrEmpty));

    static void ValidateScreenVersion(SemanticApplication application, SemanticVersion version) =>
        ValidateFeatureVersion(
            UsesScreens(application),
            version,
            SemanticVersion.V42,
            "An ESM v42 model must declare a screen.",
            "Screens require ESM v42.");

    static void ValidateFreeScreenVersion(SemanticApplication application, SemanticVersion version) =>
        ValidateFeatureVersion(
            UsesFreeScreens(application),
            version,
            SemanticVersion.V56,
            "An ESM v56 model must declare a free screen or a screen adoption.",
            "Free screens and screen adoptions require ESM v56.");

    static void ValidateExtensionComponentVersion(SemanticApplication application, SemanticVersion version) =>
        ValidateFeatureVersion(
            UsesExtensionComponents(application),
            version,
            SemanticVersion.V52,
            "An ESM v52 model must place an extension component on a screen.",
            "Extension components on screens require ESM v52.");

    private sealed partial class ValidationContext
    {
        readonly Dictionary<SemanticId, SemanticScreen> _screens = [];

        void RegisterScreens(SemanticSlice slice)
        {
            RequireObjects(slice.Screens, nameof(slice.Screens), "screen");
            RejectDuplicateNames(slice.Screens.Select(_ => _.Name), $"screen in slice '{slice.Name}'");
            foreach (var screen in slice.Screens)
            {
                Register(screen.Id, screen.Name, "screen");
                _screens.Add(screen.Id, screen);
            }
        }

        void RegisterFreeScreens(SemanticApplication application)
        {
            RequireObjects(application.Screens, nameof(application.Screens), "free screen");
            foreach (var screen in application.Screens)
            {
                Register(screen.Id, screen.Name, "free screen");
                _screens.Add(screen.Id, screen);
            }
        }

        void ValidateFreeScreens(SemanticApplication application)
        {
            foreach (var screen in application.Screens)
            {
                ValidateOwner(screen.Id, () => ValidateScreen(screen));
            }
        }

        void ValidateScreens(SemanticSlice slice)
        {
            foreach (var screen in slice.Screens)
            {
                ValidateOwner(screen.Id, () => ValidateScreen(screen));
                if (!screen.Needs.IsDefaultOrEmpty) throw new InvalidSemanticContract($"Screen '{screen.Name}' of slice '{slice.Name}' needs data nothing can provide; only a free screen has needs.");
            }

            RequireObjects(slice.Adoptions, nameof(slice.Adoptions), "screen adoption");
            var adopted = new HashSet<SemanticId>();
            foreach (var adoption in slice.Adoptions)
            {
                if (!adopted.Add(adoption.Screen)) throw new InvalidSemanticContract($"Slice '{slice.Name}' adopts screen '{adoption.Screen}' more than once.");
                ValidateAdoption(slice, adoption);
            }
        }

        void ValidateAdoption(SemanticSlice slice, SemanticScreenAdoption adoption)
        {
            var screen = _eventRoutesApplication.Screens.FirstOrDefault(candidate => candidate.Id == adoption.Screen)
                ?? throw new InvalidSemanticContract($"Slice '{slice.Name}' adopts unknown free screen '{adoption.Screen}'.");
            RequireObjects(adoption.Provisions, nameof(adoption.Provisions), "screen provision");
            var provided = new HashSet<string>(StringComparer.Ordinal);
            foreach (var provision in adoption.Provisions)
            {
                var need = screen.Needs.FirstOrDefault(candidate => candidate.Name == provision.Need)
                    ?? throw new InvalidSemanticContract($"Slice '{slice.Name}' provides '{provision.Need}', which screen '{screen.Name}' does not need.");
                if (!provided.Add(provision.Need)) throw new InvalidSemanticContract($"Slice '{slice.Name}' provides need '{provision.Need}' of screen '{screen.Name}' more than once.");
                if (!_queries.TryGetValue(provision.Query, out var query)) throw new InvalidSemanticContract($"Slice '{slice.Name}' provides need '{provision.Need}' of screen '{screen.Name}' from unknown query '{provision.Query}'.");
                if (query.Cardinality == SemanticQueryCardinality.Many != need.Many)
                {
                    throw new InvalidSemanticContract($"Slice '{slice.Name}' provides need '{provision.Need}' of screen '{screen.Name}' from query '{query.Name}', which does not return {(need.Many ? "a list" : "a single value")}.");
                }

                RequireObjects(provision.Fields, nameof(provision.Fields), "screen provision field");
                foreach (var field in provision.Fields)
                {
                    RequireText(field.Field, $"Screen '{screen.Name}' provision field");
                    RequireText(field.Path, $"Screen '{screen.Name}' provision path");
                }
            }

            foreach (var need in screen.Needs.Where(need => !provided.Contains(need.Name)))
            {
                throw new InvalidSemanticContract($"Slice '{slice.Name}' adopts screen '{screen.Name}' but does not provide its need '{need.Name}' of shape '{need.Shape}'.");
            }
        }

        void ValidateScreenNeeds(SemanticScreen screen, string owner)
        {
            if (screen.Needs.IsDefaultOrEmpty) return;
            RequireObjects(screen.Needs, nameof(screen.Needs), "screen need");
            RejectDuplicateNames(screen.Needs.Select(_ => _.Name), $"need of screen '{screen.Name}'");
            foreach (var need in screen.Needs)
            {
                RequireText(need.Name, $"{owner} need");
                RequireText(need.Shape, $"{owner} need shape");
                RequireObjects(need.Fields, nameof(need.Fields), "screen need field");
                foreach (var field in need.Fields) RequireText(field.Property, $"{owner} need field");
            }
        }

        void ValidateScreen(SemanticScreen screen)
        {
            var owner = $"Screen '{screen.Name}'";
            RequireText(screen.Title, $"{owner} title");
            RequireObjects(screen.Data, nameof(screen.Data), "screen data");
            RequireObjects(screen.Actions, nameof(screen.Actions), "screen action");
            RequireObjects(screen.Navigation, nameof(screen.Navigation), "screen navigation");
            if (screen.Layout is null) throw new InvalidSemanticContract($"{owner} has no layout.");
            foreach (var data in screen.Data) ValidateScreenData(data, owner);
            foreach (var action in screen.Actions) ValidateScreenAction(action, owner);
            foreach (var navigation in screen.Navigation)
            {
                RequireScreen(navigation.Screen, owner);
                RequireText(navigation.Trigger, $"{owner} navigation trigger");
            }

            ValidateScreenNeeds(screen, owner);
            ValidateScreenComponents(screen, owner);
            RequireArray(screen.Layout.Fits, "fits");
            foreach (var fit in screen.Layout.Fits) RequireText(fit, $"{owner} fit");
            ValidateRegions(screen.Layout.Regions, owner);
        }

        void ValidateScreenComponents(SemanticScreen screen, string owner)
        {
            if (screen.Components.IsDefaultOrEmpty && screen.Extensions.IsDefaultOrEmpty) return;
            RequireObjects(screen.Extensions, nameof(screen.Extensions), "screen extension");
            RequireObjects(screen.Components, nameof(screen.Components), "screen component");
            RejectDuplicateNames(screen.Components.Select(_ => _.Name), $"component on screen '{screen.Name}'");
            var needed = new HashSet<SemanticId>();
            foreach (var extension in screen.Extensions)
            {
                RequireText(extension.Reference, $"{owner} extension reference");
                if (!_eventRoutesApplication.Systems.Any(system => system.Id == extension.System)) throw new InvalidSemanticContract($"{owner} needs the extension of unknown system '{extension.System}'.");
                if (!needed.Add(extension.System)) throw new InvalidSemanticContract($"{owner} lists the extension of system '{extension.System}' more than once.");
            }

            foreach (var component in screen.Components)
            {
                RequireText(component.Name, $"{owner} component name");
                RequireText(component.Component, $"{owner} component");
                if (!needed.Contains(component.System)) throw new InvalidSemanticContract($"{owner} places component '{component.Name}' through a system whose extension it does not list.");
                RequireArray(component.Properties, "properties");
                foreach (var property in component.Properties) RequireText(property, $"{owner} component property");
                RequireObjects(component.Handles, nameof(component.Handles), "screen signal handler");
                foreach (var handler in component.Handles)
                {
                    RequireText(handler.Signal, $"{owner} signal");
                    RequireArray(handler.Commands, "commands");
                    foreach (var command in handler.Commands)
                    {
                        if (!_commands.ContainsKey(command)) throw new InvalidSemanticContract($"{owner} handles signal '{handler.Signal}' with unknown command '{command}'.");
                    }
                }
            }
        }

        void ValidateScreenData(SemanticScreenData data, string owner)
        {
            if (!_queries.ContainsKey(data.Query)) throw new InvalidSemanticContract($"{owner} shows unknown query '{data.Query}'.");
            RequireObjects(data.Fields, nameof(data.Fields), "screen field");
            foreach (var field in data.Fields) RequireText(field.Property, $"{owner} field");
        }

        void ValidateScreenAction(SemanticScreenAction action, string owner)
        {
            if (!_commands.TryGetValue(action.Command, out var command)) throw new InvalidSemanticContract($"{owner} issues unknown command '{action.Command}'.");
            RequireText(action.Label, $"{owner} action label");
            if (!_screenSubjects.Contains(action.Subject, StringComparer.Ordinal)) throw new InvalidSemanticContract($"{owner} action subject '{action.Subject}' is unknown.");
            if (action.Then is { } then) RequireScreen(then, owner);
            RequireObjects(action.Inputs, nameof(action.Inputs), "screen input");
            var properties = command.Properties.Select(property => property.Id).ToHashSet();
            var seen = new HashSet<SemanticId>();
            foreach (var input in action.Inputs)
            {
                if (!properties.Contains(input.Property) || !seen.Add(input.Property))
                {
                    throw new InvalidSemanticContract($"{owner} binds input '{input.Property}' that is not a distinct property of command '{command.Name}'.");
                }
            }
        }

        void ValidateRegions(ImmutableArray<SemanticScreenRegion> regions, string owner)
        {
            RequireObjects(regions, "regions", "screen region");
            foreach (var region in regions)
            {
                RequireText(region.Kind, $"{owner} region kind");
                RequireArray(region.Actions, "actions");
                ValidateRegions(region.Regions, owner);
            }
        }

        void RequireScreen(SemanticId screen, string owner)
        {
            if (!_screens.ContainsKey(screen)) throw new InvalidSemanticContract($"{owner} leads to unknown screen '{screen}'.");
        }

        void RequireText(string? value, string description)
        {
            if (string.IsNullOrEmpty(value) || !IsNfc(value)) throw new InvalidSemanticContract($"The {description} must be non-empty, well-formed Unicode NFC text.");
        }
    }
}
