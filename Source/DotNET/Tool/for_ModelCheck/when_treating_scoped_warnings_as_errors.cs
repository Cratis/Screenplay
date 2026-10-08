// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelCheck;

public class when_treating_scoped_warnings_as_errors : given.a_model
{
    int _exitCode;

    void Because() => _exitCode = ModelCheck.Run([Root, "--scope", "M.F.Warning", "--warnaserror"], Output, Error);

    [Fact] void should_fail_for_the_scoped_warning() => _exitCode.ShouldEqual(1);
    [Fact] void should_not_include_the_unrelated_error() => Output.ToString().ShouldContain("0 error(s), 1 warning(s)");
}
