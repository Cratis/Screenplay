// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_public_events_with_opaque_origins
{
    [Fact]
    void should_patch_the_public_header_without_renaming_an_origin_or_changing_identity()
    {
        const string source = "import Other.Imported from \"Shipped\" // import origin\nmodule Shipping\n  feature Orders\n    slice Translate Transfer\n      direction inbound // direction\n      public event Shipped from \"../Shipped.play\" // event origin\n        name String\n";
        var document = WorkspaceDocument.Create("shipping", PortablePlayPath.Parse("shipping.play"), Encoding.UTF8.GetBytes(source));
        var identity = ApplicationIdentity.Create("Shipping");
        var address = SemanticAddress.ForEventContract(SemanticAddress.ForSlice(identity, "Shipping", "Orders", "Transfer"), "Shipped");
        var catalog = SemanticIdentityCatalog.Create(identity, [], [new(address, SemanticIdentityCatalog.Empty(identity).ResolveSemantic(address), SemanticIdentityOrigin.Persisted)], [new(address, EventContractId.CreateLegacy(identity, "Shipped"), new(1), SemanticIdentityOrigin.Persisted)]);
        var workspace = ScreenplayWorkspace.Create("Shipping", [document], catalog);
        var target = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(entry => entry.Node is EventSyntax);
        var result = workspace.ProposeRename(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Target = target.Handle,
            ExpectedName = "Shipped",
            NewName = "Published"
        });
        Assert.True(result.Accepted, string.Join("; ", result.Conflicts.Select(conflict => conflict.Message)));
        var after = result.Workspace!;
        after.Documents.Single().Text.ShouldEqual(source.Replace("public event Shipped", "public event Published", StringComparison.Ordinal).Replace("        name String", "        id \"Shipped\"\n        name String", StringComparison.Ordinal));
        after.IdentityCatalog.EventContracts.Single().Id.ShouldEqual(workspace.IdentityCatalog.EventContracts.Single().Id);
        after.Compilation.Success.ShouldBeFalse();
    }
}
