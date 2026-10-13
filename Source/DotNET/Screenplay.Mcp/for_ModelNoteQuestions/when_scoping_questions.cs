// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_ModelNoteQuestions;

public class when_scoping_questions : given.a_model_with_questions
{
    ModelNoteQuestionsOutcome _outcome = null!;

    void Because() => _outcome = ModelNoteQuestions.Read(RootPath, Scope);

    [Fact] void should_include_the_slice_and_application_questions() => _outcome.Findings.Length.ShouldEqual(2);
    [Fact] void should_exclude_orphans() => _outcome.Findings.All(finding => finding.Address is not null).ShouldBeTrue();
    [Fact] void should_keep_the_scope() => _outcome.EffectiveScope.ShouldEqual(Scope);
    [Fact] void should_gate_the_selected_set() => _outcome.GetExitCode(true).ShouldEqual(1);
}
