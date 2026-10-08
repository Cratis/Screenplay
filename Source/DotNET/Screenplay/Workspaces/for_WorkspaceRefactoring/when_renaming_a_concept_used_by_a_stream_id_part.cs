// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_a_concept_used_by_a_stream_id_part
{
    [Theory]
    [InlineData("period", WorkspaceAuthoringFormatting.PreserveTrivia)]
    [InlineData("Period", WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)]
    void should_repair_the_type_without_renaming_the_part(string name, WorkspaceAuthoringFormatting formatting)
    {
        var source = "concept Period : String\neventsource A\n  stream S\n    streamId\n      " + name + " Period\n      key String";
        var document = WorkspaceDocument.Create("model", PortablePlayPath.Parse("model.play"), Encoding.UTF8.GetBytes(source));
        var workspace = ScreenplayWorkspace.Create("Projects", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
        var target = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(entry => entry.Node is ConceptSyntax);
        var result = workspace.ProposeRename(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Target = target.Handle,
            ExpectedName = "Period",
            NewName = "Month",
            Formatting = formatting
        });
        Assert.True(result.Accepted, string.Join('\n', result.Conflicts));
        var part = WorkspaceSyntaxIndex.Create(result.Workspace).Entries.Select(entry => entry.Node).OfType<EventStreamIdPartSyntax>().First();
        part.Name.ShouldEqual(name);
        part.Type.Name.ShouldEqual("Month");
    }
}
