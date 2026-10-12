// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_ModelNotes;

public class when_opening_duplicate_notes : given.a_notes_workspace
{
    JsonElement _opened;
    Exception? _failure;

    void Establish()
    {
        var node = System.Text.Json.Nodes.JsonNode.Parse(NotesBytes)!;
        node["notes"]!.AsArray().Add(node["notes"]![0]!.DeepClone());
        File.WriteAllText(Files.PathFor(ModelNotesStore.FileName), node.ToJsonString());
    }

    void Because()
    {
        _opened = Result(Workspaces.Open(McpJson.Empty));
        _failure = Catch.Exception(() => Workspaces.ListNotes(McpJson.Empty));
    }

    [Fact] void should_open_the_model() => _opened.GetProperty("revision").GetString().ShouldEqual(Original.Revision.ToString());
    [Fact] void should_report_unreadable_notes() => _opened.GetProperty("notes").GetProperty("status").GetString().ShouldEqual("unreadable");
    [Fact] void should_refuse_note_tools() => ((McpFailure)_failure!).FailureKind.ShouldEqual("UnreadableNotes");
}
