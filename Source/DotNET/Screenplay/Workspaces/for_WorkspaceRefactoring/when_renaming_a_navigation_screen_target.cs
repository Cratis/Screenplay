// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_a_navigation_screen_target : given.a_refactoring_workspace
{
    internal const string Source = for_ScreenplayCompiler.when_compiling_a_navigation_screen_target.Layout + """
        module Shop
          outlet sidebar extends
            item projects title "Projects" target Shop.Projects screen Projects
          feature Projects
            slice StateView Listing
              screen Projects
          feature Destination
        """;
    WorkspaceAuthoringResult _result;

    void Establish() => Workspace = ScreenplayWorkspace.Create("Shop", [Document("model", "application.play", Source)], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Shop")));
    void Because() => _result = Workspace.ProposeRename(Rename<ScreenSyntax>("Projects", "ProjectList"));

    [Fact] void should_refuse_the_partial_rename() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_explain_the_coordinated_edit() => _result.Conflicts.Single().Message.ShouldContain("coordinated typed edits");
    [Fact] void should_preserve_the_workspace() => Workspace.Documents.Single().Text.ShouldEqual(Source);
}
