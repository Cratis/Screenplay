// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelOpenQuestions;

public class when_gating_a_scope : given.a_model_with_questions
{
    int _exit;

    void Because() => _exit = ModelCheck.Run([Root, "--scope", "M.F.Clean", "--open-questions", "fail"], Output, Error);

    [Fact] void should_fail_for_questions() => _exit.ShouldEqual(1);
    [Fact] void should_include_slice_notes() => Output.ToString().Contains("May a member cancel?", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_include_ancestor_notes() => Output.ToString().Contains("Who owns the application?", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_exclude_unrelated_notes() => Output.ToString().Contains("Unrelated question", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_count_questions() => Output.ToString().Contains("2 open question(s) in scope", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_use_a_play_code() => Output.ToString().Contains("info model-note.open-question", StringComparison.Ordinal).ShouldBeTrue();
}
