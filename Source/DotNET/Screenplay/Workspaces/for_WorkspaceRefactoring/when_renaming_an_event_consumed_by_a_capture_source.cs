// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_an_event_consumed_by_a_capture_source
{
    [Fact]
    void should_patch_the_consuming_from_line()
    {
        const string source = "module Shipping\n  feature Orders\n    slice Translate Track\n      direction inbound\n      public event Dispatched from \"shipping\"\n      event Received\n      capture Feed\n        source events\n          from Dispatched\n        key id\n        append Received\n";
        var document = WorkspaceDocument.Create("shipping", PortablePlayPath.Parse("shipping.play"), Encoding.UTF8.GetBytes(source));
        var identity = ApplicationIdentity.Create("Shipping");
        var address = SemanticAddress.ForEventContract(SemanticAddress.ForSlice(identity, "Shipping", "Orders", "Track"), "Dispatched");
        var catalog = SemanticIdentityCatalog.Create(identity, [], [new(address, SemanticIdentityCatalog.Empty(identity).ResolveSemantic(address), SemanticIdentityOrigin.Persisted)], [new(address, EventContractId.CreateLegacy(identity, "Dispatched"), new(1), SemanticIdentityOrigin.Persisted)]);
        var workspace = ScreenplayWorkspace.Create("Shipping", [document], catalog);
        var target = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(entry => entry.Node is EventSyntax declared && declared.Name == "Dispatched");
        var result = workspace.ProposeRename(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Target = target.Handle,
            ExpectedName = "Dispatched",
            NewName = "Shipped"
        });
        Assert.True(result.Accepted, string.Join("; ", result.Conflicts.Select(conflict => conflict.Message)));
        result.Workspace!.Documents.Single().Text.ShouldContain("          from Shipped\n");
    }
}
