// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_ModelNoteQuestions;

public class when_no_open_questions_remain : given.a_model_with_questions
{
    ModelNoteQuestionsOutcome _outcome = null!;

    void Establish()
    {
        Notes = Notes with { Notes = [Notes.Notes[0] with { Kind = "task" }, Notes.Notes[1] with { Kind = "note" }, Notes.Notes[2] with { Resolution = new("dismissed", "Not needed", "team", DateTimeOffset.UtcNow) }] };
        WriteNotes();
    }

    void Because() => _outcome = ModelNoteQuestions.Read(RootPath);

    [Fact] void should_not_include_tasks_notes_or_resolved_questions() => _outcome.Findings.ShouldBeEmpty();
    [Fact] void should_pass_the_gate() => _outcome.GetExitCode(true).ShouldEqual(0);
    [Fact] void should_report_readable_notes() => _outcome.NotesStatus.ShouldEqual("ok");
}
