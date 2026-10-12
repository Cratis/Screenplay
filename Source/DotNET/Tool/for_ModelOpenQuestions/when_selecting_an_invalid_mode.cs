// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelOpenQuestions;

public class when_selecting_an_invalid_mode : given.a_model_with_questions
{
    int _exit;

    void Because() => _exit = ModelCheck.Run([Root, "--open-questions", "invalid"], Output, Error);

    [Fact] void should_report_a_usage_error() => _exit.ShouldEqual(2);
    [Fact] void should_describe_the_modes() => Error.ToString().Contains("report or fail", StringComparison.Ordinal).ShouldBeTrue();
}
