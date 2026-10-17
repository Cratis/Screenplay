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
        _targets = Targets(SemanticKind.Slice, typed: false);
    }

    void Because()
    {
        _proposals = [.. _targets.Select(target => ProposeNotes([new { operation = "reanchor", id = _id, target }]))];
        _notes = [.. _proposals.Select(Review)];
    }

    [Fact] void should_report_both_reanchor_substitutions() => _proposals.All(proposal => proposal.GetProperty("substitutions").GetArrayLength() == 1).ShouldBeTrue();
    [Fact] void should_report_the_requested_slice_targets() => _proposals.Select((proposal, index) => proposal.GetProperty("substitutions")[0].GetProperty("target").GetRawText() == _targets[index].GetRawText()).All(matches => matches).ShouldBeTrue();
    [Fact] void should_report_the_actual_application_anchor() => _proposals.All(proposal => proposal.GetProperty("substitutions")[0].GetProperty("anchor").GetProperty("application").GetBoolean()).ShouldBeTrue();
    [Fact] void should_keep_the_nearest_identified_owner() => _notes.All(note => note.GetProperty("lastAddress").GetProperty("kind").GetString() == "Application").ShouldBeTrue();
    [Fact] void should_report_substitution_even_without_a_notes_change() => _proposals.All(proposal => proposal.GetProperty("afterNotesRevision").GetString() == Opened.GetProperty("notesRevision").GetString()).ShouldBeTrue();
}
