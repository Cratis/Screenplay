// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_ModelNoteQuestions;

public class when_notes_are_unreadable : given.a_model_with_questions
{
    ModelNoteQuestionsOutcome _outcome = null!;

    void Establish() => File.WriteAllText(NotesPath, "<<<<<<< main");

    void Because() => _outcome = ModelNoteQuestions.Read(RootPath);

    [Fact] void should_fail_report_mode() => _outcome.GetExitCode().ShouldEqual(2);
    [Fact] void should_fail_gate_mode() => _outcome.GetExitCode(true).ShouldEqual(2);
    [Fact] void should_report_unreadable_notes() => _outcome.NotesStatus.ShouldEqual("unreadable");
    [Fact] void should_name_the_failure() => _outcome.FailureKind.ShouldEqual("UnreadableNotes");
    [Fact] void should_explain_the_failure() => _outcome.Reason.ShouldNotBeNull();
    [Fact] void should_not_repair_notes() => File.ReadAllText(NotesPath).ShouldEqual("<<<<<<< main");
    [Fact] void should_not_block_opening_the_model() => McpState.Deserialize(OriginalState).Open(Root).Compilation.Success.ShouldBeTrue();
}
