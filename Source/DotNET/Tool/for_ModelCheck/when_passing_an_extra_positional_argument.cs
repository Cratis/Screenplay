// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelCheck;

public class when_passing_an_extra_positional_argument : given.a_model
{
    int _exitCode;

    void Because() => _exitCode = ModelCheck.Run([Root, "extra"], Output, Error);

    [Fact] void should_report_a_usage_error() => _exitCode.ShouldEqual(2);
    [Fact] void should_name_the_extra_argument() => Error.ToString().ShouldContain("Unexpected argument 'extra'");
}
