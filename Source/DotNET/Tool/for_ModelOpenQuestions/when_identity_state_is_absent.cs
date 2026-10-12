// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelOpenQuestions;

public class when_identity_state_is_absent : given.a_model_with_questions
{
    int _exit;

    void Establish() => File.Delete(Path.Combine(Root, ".screenplay", "identities.json"));

    void Because() => _exit = ModelCheck.Run([Root, "--scope", "M.F.Clean", "--open-questions", "fail"], Output, Error);

    [Fact] void should_keep_the_question_gate() => _exit.ShouldEqual(1);
    [Fact] void should_report_absent_identity_state() => Output.ToString().Contains("Identity state absent", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_explain_that_notes_cannot_be_scoped() => Output.ToString().Contains("notes cannot be scoped", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_report_every_question_unscoped() => Output.ToString().Contains("3 open question(s)", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_keep_unrelated_notes_visible() => Output.ToString().Contains("Unrelated question", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_explain_orphaning() => Output.ToString().Contains("<orphaned: identity-state-absent>", StringComparison.Ordinal).ShouldBeTrue();
}
