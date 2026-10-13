// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_ModelNoteQuestions;

public class when_notes_are_absent : given.a_model_with_questions
{
    ModelNoteQuestionsOutcome _outcome = null!;

    void Establish() => File.Delete(NotesPath);

    void Because() => _outcome = ModelNoteQuestions.Read(RootPath);

    [Fact] void should_have_no_questions() => _outcome.Findings.ShouldBeEmpty();
    [Fact] void should_pass_the_gate() => _outcome.GetExitCode(true).ShouldEqual(0);
    [Fact] void should_report_absence() => _outcome.NotesStatus.ShouldEqual("absent");
    [Fact] void should_not_create_notes() => File.Exists(NotesPath).ShouldBeFalse();
}
