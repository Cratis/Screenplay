// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_ModelNoteQuestions;

public class when_identity_state_is_unreadable : given.a_model_with_questions
{
    ModelNoteQuestionsOutcome _outcome = null!;

    void Establish() => File.WriteAllText(Files.PathFor(McpState.FileName), "{}");

    void Because() => _outcome = ModelNoteQuestions.Read(RootPath);

    [Fact] void should_fail_report_mode() => _outcome.GetExitCode().ShouldEqual(2);
    [Fact] void should_report_unavailable_identities() => _outcome.IdentityStatus.ShouldEqual("unavailable");
    [Fact] void should_name_the_read_failure() => _outcome.FailureKind.ShouldEqual("ReadFailure");
    [Fact] void should_explain_the_identity_conflict() => _outcome.Reason!.ShouldContain("IdentityStateConflict");
    [Fact] void should_not_replace_identities() => File.ReadAllText(Files.PathFor(McpState.FileName)).ShouldEqual("{}");
}
