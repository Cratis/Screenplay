// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_authoring_a_json_template_without_source_metadata
{
    const string Source = """
        layout Main
          content
        module Shop
          screen template Shell
            fits slot content
            main
        """;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_accept_creating_or_replacing_a_typed_document(bool replace)
    {
        var workspace = ScreenplayWorkspace.CreateEmpty(ApplicationIdentity.Create("Shop"), "Shop");
        WorkspaceDocument? original = null;
        if (replace)
        {
            original = WorkspaceDocument.Create("shop", PortablePlayPath.Parse("shop.play"), System.Text.Encoding.UTF8.GetBytes(Source));
            workspace = ScreenplayWorkspace.Create("Shop", [original], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Shop")));
        }

        var parsed = new ScreenplayCompiler().Parse(Source).Value!;
        var typed = (ApplicationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(parsed));
        typed.Modules.Single().ScreenTemplates.Single().FitsSlotLocation.ShouldBeNull();
        var request = new WorkspaceAuthoringRequest
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Documents = replace
                ? [new ReplaceWorkspaceSyntaxDocument(original!.Id, typed)]
                : [new CreateWorkspaceSyntaxDocument("shop", PortablePlayPath.Parse("shop.play"), typed)]
        };

        var result = workspace.ProposeAuthoring(request);
        result.Accepted.ShouldBeTrue();
        result.Workspace!.Documents.Single().Text.ShouldContain("fits slot content");
    }
}
