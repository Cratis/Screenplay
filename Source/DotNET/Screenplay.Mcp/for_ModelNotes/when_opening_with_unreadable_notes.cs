// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_ModelNotes;

public class when_opening_with_unreadable_notes : given.a_notes_workspace
{
    JsonElement _opened;
    Exception? _listFailure;
    Exception? _writeFailure;

    void Establish() => File.WriteAllText(Files.PathFor(ModelNotesStore.FileName), "<<<<<<< ours\n{}\n=======\n{}\n>>>>>>> theirs");

    void Because()
    {
        _opened = Result(Workspaces.Open(McpJson.Empty));
        _listFailure = Catch.Exception(() => Workspaces.ListNotes(McpJson.Empty));
        _writeFailure = Catch.Exception(() => ProposeNotes([new { operation = "remove", id = Notes.Notes[0].Id }]));
    }

    [Fact] void should_open_the_model() => _opened.GetProperty("revision").GetString().ShouldEqual(Original.Revision.ToString());
    [Fact] void should_report_unreadable() => _opened.GetProperty("notes").GetProperty("status").GetString().ShouldEqual("unreadable");
    [Fact] void should_refuse_reads() => ((McpFailure)_listFailure!).FailureKind.ShouldEqual("UnreadableNotes");
    [Fact] void should_refuse_writes() => ((McpFailure)_writeFailure!).FailureKind.ShouldEqual("UnreadableNotes");
    [Fact] void should_never_repair() => File.ReadAllText(Files.PathFor(ModelNotesStore.FileName)).StartsWith("<<<<<<<", StringComparison.Ordinal).ShouldBeTrue();
}
