// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Validates the application shell a <c>ui profile</c> names: its root template, the shell options, the appearance and the template new screens start from.
/// </summary>
/// <remarks>
/// The rules restate Scene's <c>ShellResolver</c> and <c>TemplateApplicability</c> (the shared <c>effective-shell-fixtures.json</c> and
/// <c>template-applicability-fixtures.json</c> vectors) over what the document alone knows. With a blueprint catalog the same rules also decide
/// over what a blueprint ships. There is no second rule set: the compiler and <see cref="UiProfileShellBinder"/> run this one.
/// </remarks>
internal static partial class ProfileShellValidator
{
    internal const string ApplicationScope = "application";
    internal const string FeatureScope = "feature";
    const int MaximumInitials = 2;
    const int MaximumImageBytes = 400 * 1024;

    static readonly string[] _colorSchemes = ["light", "dark", "system"];
    static readonly string[] _cornerStyles = ["sharp", "default", "round"];
    static readonly string[] _logoKinds = ["initials", "icon", "image"];

    // Scene's logo icons (ProfileIdentityResolver.LogoIcons): the closed set an icon logo can use.
    static readonly string[] _logoIcons = ["box", "layers", "home", "users", "receipt", "package", "database", "palette"];

    static readonly string[] _contentTypes = ["Workspace", "List", "Detail", "Form"];

    /// <summary>
    /// Validates every ui profile of the application.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to validate.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <param name="catalog">The installed blueprints, or <see langword="null"/> when the host has none to offer.</param>
    public static void Validate(ApplicationSyntax application, ParserContext context, IReadOnlyList<BlueprintDescriptor>? catalog = null)
    {
        foreach (var profile in application.UiProfiles ?? [])
        {
            // The catalog adds facts to the document's own; the document-only diagnostics are reported once, by the compiler.
            if (catalog is null)
            {
                ValidateLegacyLayout(profile, application, context);
                ValidateIdentity(profile, context);
                ValidateAppearance(profile, context);
            }

            if (profile.Root is { } root)
            {
                ValidateReference(profile, application, catalog, context, root.Name, root.Blueprint, root.Version, root.Location, isRoot: true, root);
            }

            if (profile.NewScreenTemplate is { } newScreens)
            {
                ValidateReference(profile, application, catalog, context, newScreens.Name, newScreens.Blueprint, newScreens.Version, newScreens.Location, isRoot: false, null);
            }
        }
    }

    internal static IEnumerable<TemplateDescriptor> Own(ApplicationSyntax application)
    {
        var exposures = application.Exposures.ToList();
        IReadOnlyList<string>? OptionsOf(string owner)
        {
            var own = exposures.Where(exposure => exposure.Owner == owner).ToList();
            return own.Count == 0
                ? null
                : [.. own.SelectMany(exposure => exposure.Properties)
                    .SelectMany(property => new[] { property.Path, $"{property.Component}.{property.Path}", property.Label ?? property.Path })
                    .Distinct(StringComparer.Ordinal)];
        }

        foreach (var layout in application.Layouts ?? [])
        {
            yield return new TemplateDescriptor(layout.Name, TemplateDescriptorKind.Layout)
            {
                Scopes = layout.RestrictsScopes ? [.. layout.Scopes] : null,
                Type = layout.TemplateType,
                Slots = [.. layout.Slots.Select(slot => slot.Name)],
                Options = OptionsOf(layout.Name)
            };
        }

        foreach (var template in application.Modules.SelectMany(module => module.ScreenTemplates))
        {
            // An own screen template is a shell when it declares the application scope: that is how a root screen template is written.
            var scopes = template.RestrictsScopes ? template.Scopes.ToList() : null;
            yield return new TemplateDescriptor(template.Name, TemplateDescriptorKind.ScreenTemplate)
            {
                IsRoot = scopes?.Contains(ApplicationScope, StringComparer.Ordinal) == true,
                Scopes = scopes,
                Type = template.TemplateType,
                FitsSlot = template.FitsSlot is not null,
                Slots = [.. template.Slots.Select(slot => slot.Name)],
                Options = OptionsOf(template.Name)
            };
        }

        foreach (var template in application.Modules.SelectMany(module => module.DialogTemplates ?? []))
        {
            yield return new TemplateDescriptor(template.Name, TemplateDescriptorKind.DialogTemplate)
            {
                Scopes = template.RestrictsScopes ? [.. template.Scopes] : null,
                Type = template.TemplateType,
                Slots = [.. template.Slots.Select(slot => slot.Name)]
            };
        }
    }

    /// <summary>
    /// The facts <c>TemplateApplicability.ValidateLayout</c>, <c>ValidateScreenTemplate</c> and <c>ValidateDialogTemplate</c> decide by.
    /// </summary>
    internal static List<string> Applicability(TemplateDescriptor template, string scope, bool asShell)
    {
        var problems = new List<string>();
        var kind = template.Kind switch
        {
            TemplateDescriptorKind.Layout => "layout",
            TemplateDescriptorKind.ScreenTemplate => "screen template",
            _ => "dialog template"
        };
        var isRoot = template.Kind == TemplateDescriptorKind.ScreenTemplate && template.IsRoot;
        if ((template.Kind == TemplateDescriptorKind.Layout || isRoot) != (scope == ApplicationScope))
        {
            var detail = asShell
                ? "only a layout or an application-scope screen template can be the shell"
                : "a layout or an application-scope screen template can only be the shell";
            problems.Add($"it is a {kind}{(isRoot ? " with the root role" : string.Empty)}, and {detail}");
        }

        if (template.Type == "ApplicationShell" && template.Kind != TemplateDescriptorKind.Layout)
        {
            problems.Add("its type 'ApplicationShell' must be a layout");
        }

        if (template.Type == "Dialog" && template.Kind != TemplateDescriptorKind.DialogTemplate)
        {
            problems.Add("its type 'Dialog' must be a dialog template");
        }

        if (template.Kind == TemplateDescriptorKind.Layout && template.Type is not null && _contentTypes.Contains(template.Type, StringComparer.Ordinal))
        {
            problems.Add($"its type '{template.Type}' is for content, not for a layout");
        }

        if (template.Scopes is { } scopes && !scopes.Contains(scope, StringComparer.Ordinal))
        {
            problems.Add($"it does not declare scope '{scope}'");
        }

        if (isRoot && template.FitsSlot)
        {
            problems.Add("it fits a slot, and nothing contains the application shell");
        }

        return problems;
    }

    static void ValidateLegacyLayout(UiProfileSyntax profile, ApplicationSyntax application, ParserContext context)
    {
        if (profile.Layout is not { } layout)
        {
            return;
        }

        var consequence = profile.Root is null
            ? $"write 'root template {layout}'"
            : $"the root template '{profile.Root.Name}' is used and this line is ignored - remove it";
        context.Warning(
            DiagnosticCodes.LegacyProfileLayout,
            $"The ui profile '{profile.Name}' selects its shell with the legacy 'layout {layout}' - {consequence}",
            profile.Location,
            DiagnosticArguments.Empty.With("profile", DiagnosticArgument.Text(profile.Name)).With("template", DiagnosticArgument.Text(layout)));
    }

    static void ValidateIdentity(UiProfileSyntax profile, ParserContext context)
    {
        if (profile.Identity is not { } identity)
        {
            return;
        }

        void Refuse(string message, string value, SourceLocation? location) =>
            context.Error(
                DiagnosticCodes.InvalidAppearanceValue,
                message,
                location ?? identity.Location,
                DiagnosticArguments.Empty.With("profile", DiagnosticArgument.Text(profile.Name)).With("value", DiagnosticArgument.Text(value.Length > 40 ? value[..40] : value)));

        if (identity.Name is { } name && string.IsNullOrWhiteSpace(name))
        {
            Refuse($"The identity name of ui profile '{profile.Name}' is blank - leave 'name' out to take the application's name", name, identity.NameLocation);
        }

        if (identity.LogoKind is not { } kind || identity.LogoValue is not { } value)
        {
            return;
        }

        if (!_logoKinds.Contains(kind, StringComparer.Ordinal))
        {
            Refuse($"The logo '{kind}' of ui profile '{profile.Name}' is not one of initials, icon or image", kind, identity.LogoLocation);
            return;
        }

        switch (kind)
        {
            case "initials" when value.Trim().EnumerateRunes().Count() is 0 or > MaximumInitials:
                Refuse($"The logo initials '{value}' of ui profile '{profile.Name}' must be 1 to {MaximumInitials} characters", value, identity.LogoLocation);
                break;
            case "icon" when !_logoIcons.Contains(value, StringComparer.Ordinal):
                Refuse($"The logo icon '{value}' of ui profile '{profile.Name}' is not one of {string.Join(", ", _logoIcons)}", value, identity.LogoLocation);
                break;
            case "image":
                var image = LogoImage().Match(value);
                if (!image.Success || DecodedSize(image.Groups["data"].Value) < 0)
                {
                    Refuse($"The logo image of ui profile '{profile.Name}' must be a PNG, JPG, SVG or WebP as a base64 data URI", "image", identity.LogoLocation);
                }
                else if (DecodedSize(image.Groups["data"].Value) > MaximumImageBytes)
                {
                    Refuse($"The logo image of ui profile '{profile.Name}' is larger than {MaximumImageBytes / 1024} KB", "image", identity.LogoLocation);
                }

                break;
        }
    }

    static int DecodedSize(string base64)
    {
        if (base64.Length % 4 != 0)
        {
            return -1;
        }

        var padding = base64.TakeLast(2).Count(character => character == '=');
        return (base64.Length / 4 * 3) - padding;
    }

    static void ValidateAppearance(UiProfileSyntax profile, ParserContext context)
    {
        if (profile.Appearance is not { } appearance)
        {
            return;
        }

        if (appearance.ColorScheme is { } scheme && !_colorSchemes.Contains(scheme, StringComparer.Ordinal))
        {
            context.Error(
                DiagnosticCodes.InvalidAppearanceValue,
                $"The color scheme '{scheme}' of ui profile '{profile.Name}' is not one of light, dark or system",
                appearance.ColorSchemeLocation ?? appearance.Location,
                DiagnosticArguments.Empty.With("profile", DiagnosticArgument.Text(profile.Name)).With("value", DiagnosticArgument.Text(scheme)));
        }

        if (appearance.Corners is { } corners && !_cornerStyles.Contains(corners, StringComparer.Ordinal))
        {
            context.Error(
                DiagnosticCodes.InvalidAppearanceValue,
                $"The corners '{corners}' of ui profile '{profile.Name}' are not one of sharp, default or round",
                appearance.CornersLocation ?? appearance.Location,
                DiagnosticArguments.Empty.With("profile", DiagnosticArgument.Text(profile.Name)).With("value", DiagnosticArgument.Text(corners)));
        }

        if (appearance.Accent is { } accent && !HexColor().IsMatch(accent))
        {
            context.Error(
                DiagnosticCodes.InvalidAppearanceValue,
                $"The accent '{accent}' of ui profile '{profile.Name}' is not a hex colour - write \"#RGB\", \"#RRGGBB\" or \"#RRGGBBAA\"",
                appearance.AccentLocation ?? appearance.Location,
                DiagnosticArguments.Empty.With("profile", DiagnosticArgument.Text(profile.Name)).With("value", DiagnosticArgument.Text(accent)));
        }
    }

    static void ValidateReference(
        UiProfileSyntax profile,
        ApplicationSyntax application,
        IReadOnlyList<BlueprintDescriptor>? catalog,
        ParserContext context,
        string name,
        string? blueprint,
        string? version,
        SourceLocation location,
        bool isRoot,
        RootTemplateSyntax? root)
    {
        var role = isRoot ? "root template" : "new screens use template";
        var template = FindTemplate(profile, application, catalog, context, name, blueprint, version, location, role);
        if (template is null)
        {
            return;
        }

        var scope = isRoot ? ApplicationScope : FeatureScope;
        var problems = Applicability(template, scope, isRoot);
        if (problems.Count > 0)
        {
            context.Error(
                isRoot ? DiagnosticCodes.RootTemplateNotApplicable : DiagnosticCodes.NewScreenTemplateNotApplicable,
                isRoot
                    ? $"The {role} '{name}' of ui profile '{profile.Name}' cannot be the application's shell: {string.Join("; ", problems)}"
                    : $"The {role} '{name}' of ui profile '{profile.Name}' cannot start a screen in a feature: {string.Join("; ", problems)}",
                location,
                TemplateArguments(profile, name, blueprint));
            return;
        }

        if (root is null)
        {
            return;
        }

        if (!template.Slots.Contains(template.PrimaryOutlet, StringComparer.Ordinal))
        {
            context.Error(
                DiagnosticCodes.RootTemplateWithoutContentOutlet,
                $"The root template '{name}' has no '{template.PrimaryOutlet}' outlet, so no screen can be placed in it - add a line '{template.PrimaryOutlet}' to it",
                location,
                TemplateArguments(profile, name, blueprint).With("outlet", DiagnosticArgument.Text(template.PrimaryOutlet)));
        }

        ValidateOptions(profile, root, template, context);
    }

    static TemplateDescriptor? FindTemplate(
        UiProfileSyntax profile,
        ApplicationSyntax application,
        IReadOnlyList<BlueprintDescriptor>? catalog,
        ParserContext context,
        string name,
        string? blueprint,
        string? version,
        SourceLocation location,
        string role)
    {
        if (blueprint is null)
        {
            if (catalog is not null)
            {
                return null;
            }

            var own = Own(application).FirstOrDefault(candidate => candidate.Name == name);
            if (own is null)
            {
                context.Error(
                    DiagnosticCodes.UnknownRootTemplate,
                    $"Unknown {role} '{name}' in ui profile '{profile.Name}' - declare it with 'layout {name}' or 'screen template {name}', or take it from a blueprint with '{role} blueprint <Blueprint> {name}'",
                    location,
                    TemplateArguments(profile, name, null));
            }

            return own;
        }

        if (!profile.Packages.Contains(blueprint, StringComparer.Ordinal))
        {
            if (catalog is null)
            {
                context.Error(
                    DiagnosticCodes.UnknownRootBlueprint,
                    $"The {role} '{name}' comes from blueprint '{blueprint}', which ui profile '{profile.Name}' does not list - add it to the profile's 'packages'",
                    location,
                    TemplateArguments(profile, name, blueprint));
            }

            return null;
        }

        if (catalog is null)
        {
            return null;
        }

        var installed = catalog.FirstOrDefault(candidate => candidate.Name == blueprint);
        if (installed is null)
        {
            context.Error(
                DiagnosticCodes.UnknownRootBlueprint,
                $"The {role} '{name}' comes from blueprint '{blueprint}', which is not installed",
                location,
                TemplateArguments(profile, name, blueprint));
            return null;
        }

        var found = installed.Templates.FirstOrDefault(candidate => candidate.Name == name);
        if (found is null)
        {
            context.Error(
                DiagnosticCodes.UnknownRootTemplate,
                $"Blueprint '{blueprint}' {installed.Version} ships no template '{name}' for the {role} of ui profile '{profile.Name}'",
                location,
                TemplateArguments(profile, name, blueprint));
            return null;
        }

        var pin = string.IsNullOrWhiteSpace(version) ? $"^{installed.Version}" : version.Trim();
        if (!TemplateVersionRange.IsSatisfiedBy(installed.Version, pin))
        {
            context.Warning(
                DiagnosticCodes.RootTemplateVersionNotSatisfied,
                $"The {role} '{name}' pins blueprint '{blueprint}' {pin}, but version {installed.Version} is installed",
                location,
                TemplateArguments(profile, name, blueprint).With("version", DiagnosticArgument.Text(pin)));
        }

        return found;
    }

    static void ValidateOptions(UiProfileSyntax profile, RootTemplateSyntax root, TemplateDescriptor template, ParserContext context)
    {
        if (template.Options is not { } exposed)
        {
            return;
        }

        foreach (var option in root.Options.Where(option => !exposed.Contains(option.Name, StringComparer.Ordinal)))
        {
            var known = exposed.Count == 0 ? "exposes no options" : $"exposes {string.Join(", ", exposed.Select(name => $"'{name}'"))}";
            context.Warning(
                DiagnosticCodes.UnknownShellOption,
                $"The root template '{root.Name}' of ui profile '{profile.Name}' has no option '{option.Name}' - it {known}",
                option.Location,
                TemplateArguments(profile, root.Name, root.Blueprint).With("option", DiagnosticArgument.Text(option.Name)));
        }
    }

    static DiagnosticArguments TemplateArguments(UiProfileSyntax profile, string name, string? blueprint)
    {
        var arguments = DiagnosticArguments.Empty.With("profile", DiagnosticArgument.Text(profile.Name)).With("template", DiagnosticArgument.Text(name));
        return blueprint is null ? arguments : arguments.With("blueprint", DiagnosticArgument.Text(blueprint));
    }

    [GeneratedRegex("^#(?:[0-9A-Fa-f]{3}|[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$", RegexOptions.NonBacktracking)]
    private static partial Regex HexColor();

    [GeneratedRegex(@"^data:image/(?:png|jpeg|jpg|svg\+xml|webp);base64,(?<data>[A-Za-z0-9+/]*={0,2})$", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.NonBacktracking)]
    private static partial Regex LogoImage();
}
