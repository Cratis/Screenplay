// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelCheck;

public class when_selecting_an_unknown_completeness_check : given.a_model
{
    int _exitCode;

    void Because() => _exitCode = ModelCheck.Run([Root, "--check", "unknown"], Output, Error);

    [Fact] void should_report_a_usage_error() => _exitCode.ShouldEqual(2);
    [Fact] void should_not_compile() => Output.ToString().ShouldBeEmpty();
}
