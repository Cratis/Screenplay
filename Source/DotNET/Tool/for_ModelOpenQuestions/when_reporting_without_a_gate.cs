// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelOpenQuestions;

public class when_reporting_without_a_gate : given.a_model_with_questions
{
    int _exit;

    void Because() => _exit = ModelCheck.Run([Root, "--open-questions", "report"], Output, Error);

    [Fact] void should_not_fail_for_questions() => _exit.ShouldEqual(0);
    [Fact] void should_report_every_question() => Output.ToString().Contains("3 open question(s)", StringComparison.Ordinal).ShouldBeTrue();
}
