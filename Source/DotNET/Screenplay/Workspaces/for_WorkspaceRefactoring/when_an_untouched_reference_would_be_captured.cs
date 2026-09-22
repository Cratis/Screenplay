// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_an_untouched_reference_would_be_captured
{
    [Theory]
    [InlineData(WorkspaceAuthoringReferencePolicy.Safe)]
    [InlineData(WorkspaceAuthoringReferencePolicy.Draft)]
    public void should_reject_capture_of_an_unchanged_imported_reference(WorkspaceAuthoringReferencePolicy policy)
    {
        const string source = "import External.Foo\nmodule App\n  feature F\n    slice StateChange S\n      command Create\n        name Foo";
        var document = WorkspaceDocument.Create("model", PortablePlayPath.Parse("model.play"), Encoding.UTF8.GetBytes(source));
        var before = ScreenplayWorkspace.Create("App", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("App")));
        var result = before.ProposeAuthoring(new()
        {
            ExpectedRevision = before.Revision,
            ExpectedCatalogRevision = before.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            ReferencePolicy = policy,
            Documents = [new ReplaceWorkspaceSyntaxDocument(document.Id, new ScreenplayCompiler().Parse($"concept Foo : String\n{source}").Value!)]
        });
        result.Accepted.ShouldBeFalse();
        result.WritePlan.ShouldBeNull();
        result.Conflicts.Single().Message.Contains("unchanged reference text", StringComparison.Ordinal).ShouldBeTrue();
    }
}
