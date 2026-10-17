// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_ModelNotes;

public class when_readding_retired_unbound_containers : given.an_unbound_persisted_notes_workspace
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_resolve_only_the_exact_recreated_identity(bool renameFirst)
    {
        Apply(ProposeNote(Targets(SemanticKind.Slice)[0]));
        Refresh();
        if (renameFirst)
        {
            var rename = Result(Workspaces.Rename(Arguments(new
            {
                expectedRevision = Opened.GetProperty("revision").GetString(), expectedCatalogRevision = Opened.GetProperty("catalogRevision").GetString(),
                target = McpAstHandles.Describe(Index.Entries.Single(entry => entry.Address?.Kind == SemanticKind.Slice).Handle), expectedName = "PlaceOrder", newName = "Renamed", formatting = "CanonicalizeTouchedDocuments"
            })));
            Assert.True(rename.GetProperty("success").GetBoolean(), rename.GetRawText());
            Apply(rename);
            Refresh();
        }

        var notes = Files.Read(ModelNotesStore.FileName);
        var note = ModelNotesStore.ReadBytes(notes, Split.IdentityCatalog.Application).Document!.Notes.Single();
        var slice = Split.IdentityCatalog.Semantics.Single(assignment => assignment.Address.Kind == SemanticKind.Slice);
        var source = Split.Documents.Single(document => document.Path.Value == "Orders/PlaceOrder.play").Text;
        const string other = """
            slice StateChange Other
              command Other
                key String identifier
                reads Inventory by key
                produces OtherRecorded
              event OtherRecorded
              readmodel Inventory
                key String key
            """;
        var removed = ReplaceSlice(other, [McpSemanticAddresses.Describe(slice.Address)]);
        Assert.True(removed.GetProperty("success").GetBoolean(), removed.GetRawText());
        removed.GetProperty("orphanedNotes")[0].GetString().ShouldEqual(note.Id);
        Apply(removed);
        Refresh();
        ModelNotesAnchors.Items(ModelNotesStore.ReadBytes(notes, Split.IdentityCatalog.Application).Document!, Split).Single().State.ShouldEqual("orphaned");
        var temporary = Split.IdentityCatalog.Semantics.Single(assignment => assignment.Address.Kind == SemanticKind.Slice);
        var restored = ReplaceSlice(source, [McpSemanticAddresses.Describe(temporary.Address)]);
        Assert.True(restored.GetProperty("success").GetBoolean(), restored.GetRawText());
        Apply(restored);
        Refresh();
        var recreated = Split.IdentityCatalog.Semantics.Single(assignment => assignment.Address.Kind == SemanticKind.Slice);
        recreated.Id.ShouldEqual(SemanticId.Create(slice.Address));
        recreated.Origin.ShouldEqual(SemanticIdentityOrigin.LegacyBootstrap);
        var item = ModelNotesAnchors.Items(ModelNotesStore.ReadBytes(notes, Split.IdentityCatalog.Application).Document!, Split).Single();
        item.State.ShouldEqual(renameFirst ? "orphaned" : "open");
        McpManagedFiles.Equal(Files.Read(ModelNotesStore.FileName), notes).ShouldBeTrue();
        if (renameFirst)
        {
            Apply(ProposeNotes([new { operation = "reanchor", id = note.Id, target = Targets(SemanticKind.Slice)[0] }]));
            Refresh();
            ModelNotesAnchors.Items(ModelNotesStore.ReadBytes(Files.Read(ModelNotesStore.FileName), Split.IdentityCatalog.Application).Document!, Split).Single().State.ShouldEqual("open");
        }
    }
}
