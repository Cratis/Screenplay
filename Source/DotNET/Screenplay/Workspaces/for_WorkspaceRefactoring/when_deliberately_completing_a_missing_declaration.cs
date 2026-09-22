// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_deliberately_completing_a_missing_declaration
{
    [Theory]
    [InlineData(WorkspaceAuthoringReferencePolicy.Safe)]
    [InlineData(WorkspaceAuthoringReferencePolicy.Draft)]
    public void should_allow_an_explicit_new_declaration_to_complete_reference_debt(WorkspaceAuthoringReferencePolicy policy)
    {
        const string source = "module App\n  feature F\n    slice StateChange S\n      command Create\n        name MissingType";
        var document = WorkspaceDocument.Create("model", PortablePlayPath.Parse("model.play"), Encoding.UTF8.GetBytes(source));
        var before = ScreenplayWorkspace.Create("App", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("App")));
        var result = before.ProposeAuthoring(new()
        {
            ExpectedRevision = before.Revision,
            ExpectedCatalogRevision = before.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            ReferencePolicy = policy,
            Documents = [new ReplaceWorkspaceSyntaxDocument(document.Id, new ScreenplayCompiler().Parse($"concept MissingType : String\n{source}").Value!)]
        });
        Assert.True(result.Accepted, string.Join(Environment.NewLine, result.Conflicts.Select(conflict => conflict.Message)));
        new WorkspaceReferenceBindings(WorkspaceSyntaxIndex.Create(result.Workspace!)).Bindings.All(binding => binding.Target is not null).ShouldBeTrue();
    }
}
