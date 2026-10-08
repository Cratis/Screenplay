// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelCheck;

public class when_selecting_completeness_checks_with_source_errors : given.a_model
{
    int _exitCode;

    void Because() => _exitCode = ModelCheck.Run([Root, "--scope", "M.F.Clean", "--check", "all"], Output, Error);

    [Fact] void should_preserve_the_clean_scoped_exit_code() => _exitCode.ShouldEqual(0);
    [Fact] void should_explain_why_checks_are_skipped() => Output.ToString().ShouldContain("completeness checks skipped: the model has");
}
