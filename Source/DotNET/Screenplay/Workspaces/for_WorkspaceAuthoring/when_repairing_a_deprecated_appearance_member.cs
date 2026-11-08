// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_repairing_a_deprecated_appearance_member : Specification
{
    const string Source =
        """
        design system D
          modes light
          default theme T
          tokens
            color
              surface
            radius
              control
            font
              sans

        theme T
          implements D
          values
            radius.control = "6px"
            font.sans      = "inter"
          light
            color.surface = "#FFFFFF"

        ui profile P
          appearance
            color scheme light
            accent "#4F46E5"
            corners round
            font source-serif-4
        """;

    ScreenplayWorkspace _workspace;
    WorkspaceAuthoringResult _accent;
    ApplicationSyntax _afterAccent;

    void Because()
    {
        _workspace = Create(Source);
        var index = WorkspaceSyntaxIndex.Create(_workspace);
        var diagnostic = index.RepairableDiagnostics.Single(value => value.Code == DiagnosticCodes.DeprecatedAppearanceMember && value.Message.StartsWith("'accent'", StringComparison.Ordinal));
        var repair = WorkspaceDiagnosticRepairs.Find(index, _workspace.Revision, diagnostic).Single();
        _accent = WorkspaceDiagnosticRepairs.ProposeRepair(_workspace, diagnostic.Code, repair.Subject, new()
        {
            ExpectedRevision = _workspace.Revision,
            ExpectedCatalogRevision = _workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments
        });
        _afterAccent = (ApplicationSyntax)WorkspaceSyntaxIndex.Create(_accent.Workspace!).Entries.Select(entry => entry.Node).OfType<ApplicationSyntax>().First();
    }

    [Fact] void should_accept_the_repair() => _accent.Accepted.ShouldBeTrue();
    [Fact] void should_move_the_accent_into_the_selected_themes_palette() => (_afterAccent.Themes!.Single().Palette.Single().Name, _afterAccent.Themes!.Single().Palette.Single().Value).ShouldEqual(("accent", "#4F46E5"));
    [Fact] void should_move_the_corners_into_the_radius_token_unless_the_theme_sets_it() => _afterAccent.Themes!.Single().Values.Single(value => value.Name == "radius.control").Value.ShouldEqual("6px");
    [Fact] void should_move_the_font_unless_the_theme_sets_it() => _afterAccent.Themes!.Single().Values.Single(value => value.Name == "font.sans").Value.ShouldEqual("inter");
    [Fact] void should_drop_every_deprecated_line() => (_afterAccent.UiProfiles!.Single().Appearance!.Accent, _afterAccent.UiProfiles!.Single().Appearance!.Corners, _afterAccent.UiProfiles!.Single().Appearance!.Font).ShouldEqual((null, null, null));
    [Fact] void should_keep_the_color_scheme() => _afterAccent.UiProfiles!.Single().Appearance!.ColorScheme.ShouldEqual("light");
    [Fact] void should_leave_no_deprecation_warning() => _accent.Workspace!.Compilation.Diagnostics.Any(value => value.Code == DiagnosticCodes.DeprecatedAppearanceMember).ShouldBeFalse();

    static ScreenplayWorkspace Create(string source) => ScreenplayWorkspace.Create(
        "Repair",
        [WorkspaceDocument.Create("repair", PortablePlayPath.Parse("repair.play"), Encoding.UTF8.GetBytes(source))],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Repair")));
}
