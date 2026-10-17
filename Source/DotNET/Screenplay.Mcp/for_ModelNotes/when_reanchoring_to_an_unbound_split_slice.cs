// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Mcp.for_ModelNotes;

public class when_reanchoring_to_an_unbound_split_slice : given.a_split_notes_workspace
{
    JsonElement[] _targets = [];
    JsonElement[] _proposals = [];
    JsonElement[] _notes = [];
    string _id = null!;

    void Establish()
    {
        OpenSplit(unbound: true);
        var added = ProposeNote(Arguments(new { application = true }));
        _id = Review(added).GetProperty("id").GetString()!;
        Apply(added);
        Opened = Result(Workspaces.Open(McpJson.Empty));
        _targets = Targets(SemanticKind.Slice);
    }

    void Because()
    {
        _proposals = [.. _targets.Select(target => ProposeNotes([new { operation = "reanchor", id = _id, target }]))];
        _notes = [.. _proposals.Select(Review)];
    }

    [Fact] void should_not_substitute_the_reanchor_target() => _proposals.All(proposal => proposal.GetProperty("substitutions").GetArrayLength() == 0).ShouldBeTrue();
    [Fact] void should_anchor_to_the_slice() => _notes.All(note => note.GetProperty("lastAddress").GetProperty("kind").GetString() == "Slice").ShouldBeTrue();
    [Fact] void should_change_the_notes_revision() => _proposals.All(proposal => proposal.GetProperty("afterNotesRevision").GetString() != Opened.GetProperty("notesRevision").GetString()).ShouldBeTrue();
}
