// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_ModelNotes;

public class when_a_batch_contains_an_invalid_operation : given.a_notes_workspace
{
    Exception? _failure;

    void Because() => _failure = Catch.Exception(() => ProposeNotes([new { operation = "edit", id = Notes.Notes[0].Id, text = "Changed" }, new { operation = "resolve", id = Notes.Notes[0].Id, resolution = new { by = "external-decision", text = "Decided", author = "author" } }]));

    [Fact] void should_refuse_the_whole_batch() => _failure.ShouldNotBeNull();
    [Fact] void should_leave_notes_untouched() => McpManagedFiles.Equal(Files.Read(ModelNotesStore.FileName), NotesBytes).ShouldBeTrue();
    [Fact] void should_not_retain_a_partial_proposal() => JsonSerializer.Serialize(Workspaces.State(McpJson.Empty), McpJson.Options).Contains("Changed", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_refuse_authoring_key_anchors() => Catch.Exception(() => ProposeNotes([new { operation = "add", kind = "note", text = "About this", author = "author", anchor = new { authoringKey = "x" } }])).ShouldNotBeNull();
    [Fact] void should_refuse_non_http_urls() => Catch.Exception(() => ProposeNotes([new { operation = "edit", id = Notes.Notes[0].Id, refs = new[] { new { id = "D123", url = "file:///secrets" } } }])).ShouldNotBeNull();
    [Fact] void should_refuse_document_targets() => Catch.Exception(() => ProposeNotes([new { operation = "add", kind = "note", text = "About this", author = "author", target = McpAstHandles.Describe(McpWorkspaceAnalysis.For(Original).Syntax.Entries.First(entry => entry.Parent is null).Handle) }])).ShouldNotBeNull();
}
