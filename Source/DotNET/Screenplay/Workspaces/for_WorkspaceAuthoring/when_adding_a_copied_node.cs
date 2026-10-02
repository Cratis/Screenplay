// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_adding_a_copied_node : Specification
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_not_copy_source_comments_from_an_existing_occurrence(bool withCopy)
    {
        const string source = "module Projects\n  feature Naming\n    slice StateChange Rename\n      event Original\n        value String // belongs to the original\n      event Other\n";
        var workspace = ScreenplayWorkspace.Create("Projects", [WorkspaceDocument.Create("source", PortablePlayPath.Parse("source.play"), Encoding.UTF8.GetBytes(source))], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var property = index.Entries.Select(entry => entry.Node).OfType<PropertySyntax>().Single();
        var destination = index.Entries.Single(entry => entry.Node is EventSyntax declaration && declaration.Name == "Other");
        var result = workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Operations = [new AddWorkspaceNode(destination.Handle, destination.Node, "properties", withCopy ? property with { Name = "copy" } : property)]
        });
        result.Conflicts.ShouldBeEmpty();
        WorkspaceSourceTokenizer.Tokenize(result.Workspace!.Documents[0]).Tokens
            .Count(token => token.Kind == WorkspaceSourceTokenKind.Comment && token.Text == "// belongs to the original").ShouldEqual(1);
        WorkspaceDroppedComments.In(result.WritePlan!).ShouldBeEmpty();
    }
}
