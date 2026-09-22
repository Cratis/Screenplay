// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_unassigned_hierarchy_into_an_existing_name
{
    [Theory]
    [InlineData("module Left\n  feature One\nmodule Right\n  feature Two", SemanticKind.Module)]
    [InlineData("module App\n  feature Left\n  feature Right", SemanticKind.Feature)]
    [InlineData("module App\n  feature Parent\n    feature Left\n    feature Right", SemanticKind.Feature)]
    public void should_refuse_to_collapse_distinct_logical_declarations(string hierarchy, SemanticKind kind)
    {
        var document = WorkspaceDocument.Create("model", PortablePlayPath.Parse("model.play"), Encoding.UTF8.GetBytes($"trigger Tick\n{hierarchy}"));
        var workspace = ScreenplayWorkspace.Create("App", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("App")));
        var target = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(entry => entry.Address?.Kind == kind && entry.Address.Name == "Left");
        target.SemanticId.ShouldBeNull();
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
        result.Conflicts.Single().Message.Contains("logical", StringComparison.Ordinal).ShouldBeTrue();
    }
}
