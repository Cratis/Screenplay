// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_an_event_targeted_by_a_projection
{
    [Fact]
    void should_refuse_instead_of_leaving_the_projection_target_dangling()
    {
        const string source = "module Shipping\n  feature Orders\n    slice Translate Publish\n      direction outbound\n      event Packed\n      public event Shipped\n        orderId String\n      projection Publisher => Shipped\n        from Packed\n          orderId = $eventSourceId\n";
        var document = WorkspaceDocument.Create("shipping", PortablePlayPath.Parse("shipping.play"), Encoding.UTF8.GetBytes(source));
        var identity = ApplicationIdentity.Create("Shipping");
        var address = SemanticAddress.ForEventContract(SemanticAddress.ForSlice(identity, "Shipping", "Orders", "Publish"), "Shipped");
        var catalog = SemanticIdentityCatalog.Create(identity, [], [new(address, SemanticIdentityCatalog.Empty(identity).ResolveSemantic(address), SemanticIdentityOrigin.Persisted)], [new(address, EventContractId.CreateLegacy(identity, "Shipped"), new(1), SemanticIdentityOrigin.Persisted)]);
        var workspace = ScreenplayWorkspace.Create("Shipping", [document], catalog);
        var target = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(entry => entry.Node is EventSyntax declared && declared.Name == "Shipped");
        var result = workspace.ProposeRename(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Target = target.Handle,
            ExpectedName = "Shipped",
            NewName = "Published"
        });
        result.Accepted.ShouldBeFalse();
        result.Conflicts.Single().Message.ShouldContain("target of a projection or reducer");
    }
}
