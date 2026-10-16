// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Mcp;

/// <summary>
/// Reads the application shell of every ui profile - the root template, its shell options, the appearance and the new screens
/// default - with the shell diagnostics (PLAY0790-PLAY0799) the compiler reports for them and the own templates each could take
/// instead. The rules are the compiler's own (<see cref="ProfileShellValidator"/>); this only presents them, so a refused edit and
/// this read agree.
/// </summary>
static class McpUiProfiles
{
    static readonly HashSet<string> _shellCodes =
    [
        DiagnosticCodes.UnknownRootTemplate,
        DiagnosticCodes.LegacyProfileLayout,
        DiagnosticCodes.UnknownRootBlueprint,
        DiagnosticCodes.RootTemplateVersionNotSatisfied,
        DiagnosticCodes.RootTemplateNotApplicable,
        DiagnosticCodes.RootTemplateWithoutContentOutlet,
        DiagnosticCodes.UnknownShellOption,
        DiagnosticCodes.InvalidAppearanceValue,
        DiagnosticCodes.DuplicateProfileRootTemplate,
        DiagnosticCodes.NewScreenTemplateNotApplicable
    ];

    internal static object Read(McpSnapshot snapshot, JsonElement arguments)
    {
        var name = McpJson.OptionalString(arguments, "name");
        var application = snapshot.Compilation.Value;
        var profiles = (application?.UiProfiles ?? []).ToArray();
        if (name is not null && profiles.All(profile => profile.Name != name))
        {
            throw new McpFailure($"Unknown ui profile '{name}'. Declared: {(profiles.Length == 0 ? "none" : string.Join(", ", profiles.Select(profile => profile.Name)))}.", -32602);
        }

        var selected = profiles.Where(profile => name is null || profile.Name == name).ToArray();
        var own = application is null ? [] : ProfileShellValidator.Own(application).ToArray();
        var diagnostics = snapshot.Compilation.Diagnostics.Where(diagnostic => _shellCodes.Contains(diagnostic.Code)).ToArray();
        return new
        {
            snapshot.Compilation.Success,
            snapshot.SourceRevision,
            profiles = selected.Select(profile => Describe(profile, diagnostics)).ToArray(),
            applicableRootTemplates = own.Where(template => ProfileShellValidator.Applicability(template, ProfileShellValidator.ApplicationScope, asShell: true).Count == 0 && template.Slots.Contains(template.PrimaryOutlet, StringComparer.Ordinal))
                .Select(Template).ToArray(),
            applicableNewScreenTemplates = own.Where(template => ProfileShellValidator.Applicability(template, ProfileShellValidator.FeatureScope, asShell: false).Count == 0)
                .Select(Template).ToArray(),
            appearance = new
            {
                colorSchemes = new[] { "light", "dark", "system" },
                accent = "#RGB, #RRGGBB or #RRGGBBAA"
            },
            basis = "Own templates only; a blueprint ships its own - name it with 'root template blueprint <Blueprint> <Name>' for a blueprint the profile lists in packages and let the diagnostics decide."
        };
    }

    static object Describe(UiProfileSyntax profile, Diagnostic[] diagnostics) => new
    {
        profile.Name,
        packages = profile.Packages.ToArray(),
        legacyLayout = profile.Layout,
        root = profile.Root is { } root
            ? new
            {
                root.Name,
                root.Blueprint,
                root.Version,
                options = root.Options.Select(option => new { option.Name, option.Value, syntaxKind = option.Kind.ToString() }).ToArray()
            }
            : null,
        appearance = profile.Appearance is { } appearance ? new { appearance.ColorScheme, appearance.Accent } : null,
        newScreenTemplate = profile.NewScreenTemplate is { } newScreens ? new { newScreens.Name, newScreens.Blueprint, newScreens.Version } : null,
        line = profile.Location.Line,
        path = profile.Location.Path,
        diagnostics = diagnostics
            .Where(diagnostic => diagnostic.Arguments.TryGetValue("profile", out var argument) && argument.Value.ValueKind == JsonValueKind.String && argument.Value.GetString() == profile.Name)
            .Select(diagnostic => new { diagnostic.Code, severity = diagnostic.Severity.ToString(), diagnostic.Message, line = diagnostic.Location.Line })
            .ToArray()
    };

    static object Template(TemplateDescriptor template) => new
    {
        template.Name,
        kind = template.Kind.ToString(),
        template.PrimaryOutlet,
        options = template.Options ?? []
    };
}
