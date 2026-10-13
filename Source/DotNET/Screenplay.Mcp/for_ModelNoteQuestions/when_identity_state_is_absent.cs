// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_ModelNoteQuestions;

public class when_identity_state_is_absent : given.a_model_with_questions
{
    ModelNoteQuestionsOutcome _outcome = null!;

    void Establish() => File.Delete(Files.PathFor(McpState.FileName));

    void Because() => _outcome = ModelNoteQuestions.Read(RootPath, Scope);

    [Fact] void should_report_absent_identities() => _outcome.IdentityStatus.ShouldEqual("absent");
    [Fact] void should_ignore_the_scope() => _outcome.EffectiveScope.ShouldBeNull();
    [Fact] void should_keep_every_question_visible() => _outcome.Findings.Length.ShouldEqual(3);
    [Fact] void should_mark_every_anchor_orphaned() => _outcome.Findings.All(finding => finding.Address is null).ShouldBeTrue();
    [Fact] void should_explain_orphaning() => _outcome.OrphanReason.ShouldEqual("identity-state-absent");
    [Fact] void should_keep_the_gate() => _outcome.GetExitCode(true).ShouldEqual(1);
    [Fact] void should_not_create_identities() => Files.Read(McpState.FileName).ShouldBeNull();
}
