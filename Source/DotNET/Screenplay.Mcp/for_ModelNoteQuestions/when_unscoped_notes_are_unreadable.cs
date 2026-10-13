// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_ModelNoteQuestions;

public class when_unscoped_notes_are_unreadable : given.a_model_with_questions
{
    ModelNoteQuestionsOutcome _outcome = null!;

    void Establish()
    {
        File.Delete(Files.PathFor(McpState.FileName));
        File.WriteAllText(NotesPath, "{}");
    }

    void Because() => _outcome = ModelNoteQuestions.Read(RootPath, Scope);

    [Fact] void should_report_absent_identities() => _outcome.IdentityStatus.ShouldEqual("absent");
    [Fact] void should_report_unreadable_notes() => _outcome.NotesStatus.ShouldEqual("unreadable");
    [Fact] void should_fail_even_report_mode() => _outcome.GetExitCode().ShouldEqual(2);
    [Fact] void should_not_create_identities() => Files.Read(McpState.FileName).ShouldBeNull();
}
