// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_reorganizing_existing_reference_debt
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void should_preserve_bindings_and_debt_across_each_layout(int depth)
    {
        var source = for_ScreenplayCompiler.given.Samples.Invoicing;
        var document = WorkspaceDocument.Create("original", PortablePlayPath.Parse("original.play"), Encoding.UTF8.GetBytes(source));
        var before = ScreenplayWorkspace.Create("Sales", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Sales")));
        var syntax = new ScreenplayCompiler().Compile(source).Value!;
        var groups = new PlayFileWriter().Expand(syntax).GroupBy(file => string.Join('/', file.RelativePath.Split(System.IO.Path.DirectorySeparatorChar).Take(depth)));
        var documents = groups.Select((group, position) => new CreateWorkspaceSyntaxDocument(
            $"layout-{position}",
            PortablePlayPath.Parse($"layout-{position}.play"),
            PlayFolderMerge.Merge([.. group.Select(file => new ScreenplayCompiler().Parse(file.Content))]).Value!)).ToArray();
        var result = before.ProposeAuthoring(new()
        {
            ExpectedRevision = before.Revision,
            ExpectedCatalogRevision = before.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Documents = [new RemoveWorkspaceDocument { Document = document.Id }, .. documents]
        });
        Assert.True(result.Accepted, string.Join(Environment.NewLine, result.Conflicts.Select(conflict => conflict.Message)));
        WorkspaceReferenceLayout.Equivalent(before, result.Workspace!).ShouldBeTrue();
        result.AuthoringDiagnostics.Any(diagnostic => diagnostic.Message.Contains("Reference debt", StringComparison.Ordinal)).ShouldBeTrue();
        result.Workspace!.Documents.Length.ShouldEqual(documents.Length);
    }
}
