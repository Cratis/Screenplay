// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_ModelNotes;

public class when_fixing_binding_after_container_notes : given.an_unbound_persisted_notes_workspace
{
    SemanticIdentityAssignment[] _before = [];
    byte[]? _notes;
    ModelNotesItem[] _items = [];

    void Establish()
    {
        Apply(ProposeNotes([.. new[] { SemanticKind.Module, SemanticKind.Feature, SemanticKind.Slice }.Select(kind => (object)new { operation = "add", kind = "note", text = kind.ToString(), author = "team", target = Targets(kind)[0] })]));
        Refresh();
        _before = [.. Split.IdentityCatalog.Semantics];
        _notes = Files.Read(ModelNotesStore.FileName);
    }

    void Because()
    {
        var source = Split.Documents.Single(document => document.Path.Value == "Orders/PlaceOrder.play").Text.Replace("reads Stock by orderId", string.Empty, StringComparison.Ordinal);
        var proposal = ReplaceSlice(source);
        Assert.True(proposal.GetProperty("success").GetBoolean(), proposal.GetRawText());
        Apply(proposal);
        Refresh();
        _items = [.. ModelNotesAnchors.Items(ModelNotesStore.ReadBytes(Files.Read(ModelNotesStore.FileName), Split.IdentityCatalog.Application).Document!, Split)];
    }

    [Fact] void should_bind_after_the_fix() => Split.Compilation.Success.ShouldBeTrue();
    [Fact] void should_preserve_each_container_id_and_origin() => _before.All(Split.IdentityCatalog.Semantics.Contains).ShouldBeTrue();
    [Fact] void should_preserve_the_sidecar_bytes() => McpManagedFiles.Equal(Files.Read(ModelNotesStore.FileName), _notes).ShouldBeTrue();
    [Fact] void should_keep_every_note_resolved() => _items.All(item => item.State == "open").ShouldBeTrue();
    [Fact] void should_keep_the_requested_containers() => _items.All(item => item.Address!.Kind == item.Note.Text).ShouldBeTrue();
}
