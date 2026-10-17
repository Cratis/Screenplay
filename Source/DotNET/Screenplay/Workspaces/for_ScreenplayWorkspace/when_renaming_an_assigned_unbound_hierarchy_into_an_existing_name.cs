// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_renaming_an_assigned_unbound_hierarchy_into_an_existing_name
{
    [Theory]
    [InlineData("module Left\n  feature One\nmodule Right\n  feature Two", SemanticKind.Module)]
    [InlineData("module App\n  feature Left\n  feature Right", SemanticKind.Feature)]
    [InlineData("module App\n  feature Parent\n    feature Left\n    feature Right", SemanticKind.Feature)]
    public void should_keep_the_logical_collapse_refusal_for_prebinding_assignments(string hierarchy, SemanticKind kind)
    {
        var document = WorkspaceDocument.Create("model", PortablePlayPath.Parse("model.play"), Encoding.UTF8.GetBytes($"import Other.Thing\n{hierarchy}"));
        var workspace = ScreenplayWorkspace.Create("App", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("App")));
        var target = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(entry => entry.Address?.Kind == kind && entry.Address.Name == "Left");
        target.SemanticId.ShouldNotBeNull();
        workspace.Compilation.Success.ShouldBeFalse();
        var result = workspace.ProposeRename(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Target = target.Handle,
            ExpectedName = "Left",
            NewName = "Right"
        });
        result.Accepted.ShouldBeFalse();
        result.Workspace.ShouldBeNull();
        result.Conflicts.Single().Message.ShouldEqual($"Cannot merge distinct logical {kind} declarations by renaming 'Left' to 'Right'.");
    }
}
