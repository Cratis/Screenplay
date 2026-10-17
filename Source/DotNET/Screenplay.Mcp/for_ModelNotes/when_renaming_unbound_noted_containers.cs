// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_ModelNotes;

public class when_renaming_unbound_noted_containers : given.an_unbound_persisted_notes_workspace
{
    [Theory]
    [InlineData(SemanticKind.Slice)]
    [InlineData(SemanticKind.Feature)]
    public void should_carry_identity_and_notes_through_rename_reopen_and_first_binding(SemanticKind kind)
    {
        var assignment = Split.IdentityCatalog.Semantics.Single(assignment => assignment.Address.Kind == kind);
        Apply(ProposeNote(Targets(kind)[0]));
        Refresh();
        var notes = Files.Read(ModelNotesStore.FileName);
        var entry = Index.Entries.First(entry => entry.Address?.Equals(assignment.Address) == true);
        var renamed = Result(Workspaces.Rename(Arguments(new
        {
            expectedRevision = Opened.GetProperty("revision").GetString(), expectedCatalogRevision = Opened.GetProperty("catalogRevision").GetString(),
            target = McpAstHandles.Describe(entry.Handle), expectedName = assignment.Address.Name, newName = "Renamed", formatting = "CanonicalizeTouchedDocuments"
        })));
        Assert.True(renamed.GetProperty("success").GetBoolean(), renamed.GetRawText());
        renamed.GetProperty("orphanedNotes").GetArrayLength().ShouldEqual(0);
        Apply(renamed);
        Refresh();
        Split.Compilation.Success.ShouldBeFalse();
        var carried = Split.IdentityCatalog.Semantics.Single(current => current.Id == assignment.Id);
        carried.Address.Name.ShouldEqual("Renamed");
        carried.Origin.ShouldEqual(SemanticIdentityOrigin.Persisted);
        var source = Split.Documents.Single(document => document.Path.Value == "Orders/PlaceOrder.play").Text.Replace("reads Stock by orderId", string.Empty, StringComparison.Ordinal);
        var fixedModel = ReplaceSlice(source);
        Assert.True(fixedModel.GetProperty("success").GetBoolean(), fixedModel.GetRawText());
        Apply(fixedModel);
        Refresh();
        Split.Compilation.Success.ShouldBeTrue();
        Split.IdentityCatalog.Semantics.ShouldContain(carried);
        McpManagedFiles.Equal(Files.Read(ModelNotesStore.FileName), notes).ShouldBeTrue();
        ModelNotesAnchors.Items(ModelNotesStore.ReadBytes(notes, Split.IdentityCatalog.Application).Document!, Split).Single().Address!.Parts[^1].Key.ShouldEqual("Renamed");
    }
}
