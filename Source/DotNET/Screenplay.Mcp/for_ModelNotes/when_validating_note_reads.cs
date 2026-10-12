// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_ModelNotes;

public class when_validating_note_reads : given.a_notes_workspace
{
    [Fact] void should_reject_unknown_views() => ((McpFailure)Catch.Exception(() => Workspaces.ListNotes(Arguments(new { view = "unknown" })))).FailureKind.ShouldEqual("InvalidArguments");
    [Fact] void should_reject_unknown_states() => ((McpFailure)Catch.Exception(() => Workspaces.ListNotes(Arguments(new { state = "unknown" })))).FailureKind.ShouldEqual("InvalidArguments");
    [Fact] void should_classify_notes_drift_separately() => ((McpFailure)Catch.Exception(() => Files.Verify(ModelNotesStore.FileName, null))).FailureKind.ShouldEqual("NotesRevisionDrift");
}
