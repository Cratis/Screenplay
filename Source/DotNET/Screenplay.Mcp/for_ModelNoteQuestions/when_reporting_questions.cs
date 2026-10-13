// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_ModelNoteQuestions;

public class when_reporting_questions : given.a_model_with_questions
{
    ModelNoteQuestionsOutcome _outcome = null!;
    byte[] _notes = [];

    void Establish() => _notes = File.ReadAllBytes(NotesPath);

    void Because() => _outcome = ModelNoteQuestions.Read(RootPath);

    [Fact] void should_report_every_question() => _outcome.Findings.Length.ShouldEqual(3);
    [Fact] void should_resolve_slice_and_application_anchors() => _outcome.Findings.Count(finding => finding.Address is not null).ShouldEqual(2);
    [Fact] void should_include_the_orphan() => _outcome.Findings.Single(finding => finding.Address is null).Message.ShouldEqual("Orphaned question");
    [Fact] void should_allow_report_mode() => _outcome.GetExitCode().ShouldEqual(0);
    [Fact] void should_fail_question_gate_mode() => _outcome.GetExitCode(failOnOpenQuestions: true).ShouldEqual(1);
    [Fact] void should_report_persisted_identities() => _outcome.IdentityStatus.ShouldEqual("persisted");
    [Fact] void should_report_readable_notes() => _outcome.NotesStatus.ShouldEqual("ok");
    [Fact] void should_preserve_notes() => File.ReadAllBytes(NotesPath).ShouldEqual(_notes);
    [Fact] void should_preserve_identities() => Files.Read(McpState.FileName).ShouldEqual(OriginalState);
}
