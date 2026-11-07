// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelCheck;

/// <summary>
/// PLAY0533 is on by default since Screenplay 6; a command line written for Screenplay 5 that names it still runs.
/// </summary>
public class when_selecting_the_check_that_is_on_by_default : given.a_model
{
    int _exitCode;

    void Because() => _exitCode = ModelCheck.Run(["--scope", "M.F.Clean", Root, "--check", "PLAY0533"], Output, Error);

    [Fact] void should_not_report_a_usage_error() => _exitCode.ShouldEqual(0);
    [Fact] void should_check_the_model() => Output.ToString().ShouldContain("in scope");
    [Fact] void should_not_complain_about_the_selection() => Error.ToString().ShouldBeEmpty();
}
