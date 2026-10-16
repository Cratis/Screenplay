// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_a_template_a_ui_profile_names : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _shell;
    WorkspaceAuthoringResult _newScreenTemplate;
    WorkspaceAuthoringResult _legacyLayout;

    void Establish() => Workspace = ScreenplayWorkspace.Create("Shop",
        [Document("model", "application.play", """
            layout AppShell
              category Shell
              content

            layout OldShell
              category Shell
              content

            layout Spare
              category Shell
              content

            module Shop
              screen template Sheet
                category Screens
                content

            ui profile Desktop
              layout OldShell

              root template AppShell
                option navigationMode slim

              new screens use template Sheet
            """)],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Shop")));

    void Because()
    {
        _shell = Workspace.ProposeRename(Rename<LayoutSyntax>("AppShell", "MainShell"));
        _newScreenTemplate = Workspace.ProposeRename(Rename<ScreenTemplateSyntax>("Sheet", "Page"));
        _legacyLayout = Workspace.ProposeRename(Rename<LayoutSyntax>("OldShell", "NewShell"));
    }

    // Layouts and templates are not rename targets at all, so a ui profile's template reference can never be left stale by a rename.
    [Fact] void should_refuse_renaming_the_root_template() => _shell.Accepted.ShouldBeFalse();
    [Fact] void should_refuse_renaming_the_new_screen_template() => _newScreenTemplate.Accepted.ShouldBeFalse();
    [Fact] void should_refuse_renaming_the_legacy_layout() => _legacyLayout.Accepted.ShouldBeFalse();
}
