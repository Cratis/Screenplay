// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelCheck;

public class when_checking_a_warning_scope : given.a_model
{
    int _exitCode;

    void Because() => _exitCode = ModelCheck.Run([Root, "--scope", "M.F.Warning"], Output, Error);

    [Fact] void should_not_fail_without_warnaserror() => _exitCode.ShouldEqual(0);
    [Fact] void should_report_the_warning() => Output.ToString().ShouldContain("0 error(s), 1 warning(s)");
}
