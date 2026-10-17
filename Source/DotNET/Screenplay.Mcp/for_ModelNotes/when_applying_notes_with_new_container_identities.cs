// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_ModelNotes;

public class when_applying_notes_with_new_container_identities : given.an_unbound_persisted_notes_workspace
{
    byte[]? _proposedState;
    byte[]? _appliedState;
    ModelNotesItem _item = null!;

    void Because()
    {
        var proposal = ProposeNote(Targets(SemanticKind.Slice)[0]);
        _proposedState = Files.Read(McpState.FileName);
        Apply(proposal);
        _appliedState = Files.Read(McpState.FileName);
        Refresh();
        _item = ModelNotesAnchors.Items(ModelNotesStore.ReadBytes(Files.Read(ModelNotesStore.FileName), Split.IdentityCatalog.Application).Document!, Split).Single();
    }

    [Fact] void should_not_persist_on_propose() => McpManagedFiles.Equal(_proposedState, EarlierState).ShouldBeTrue();
    [Fact] void should_persist_on_apply() => McpManagedFiles.Equal(_appliedState, McpState.Serialize(Split)).ShouldBeTrue();
    [Fact] void should_change_the_identity_state() => McpManagedFiles.Equal(_appliedState, EarlierState).ShouldBeFalse();
    [Fact] void should_keep_notes_resolved_after_reopen() => _item.State.ShouldEqual("open");
    [Fact] void should_keep_the_slice_anchor() => _item.Address!.Kind.ShouldEqual("Slice");
}
