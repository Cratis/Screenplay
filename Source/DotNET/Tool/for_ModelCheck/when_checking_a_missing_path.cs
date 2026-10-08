// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelCheck;

public class when_checking_a_missing_path : given.a_model
{
    int _exitCode;

    void Because() => _exitCode = ModelCheck.Run([Path.Combine(Root, "missing.play")], Output, Error);

    [Fact] void should_report_a_usage_error() => _exitCode.ShouldEqual(2);
    [Fact] void should_explain_the_missing_path() => Error.ToString().ShouldContain("does not exist");
}
