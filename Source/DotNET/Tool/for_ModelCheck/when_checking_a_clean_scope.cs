// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelCheck;

public class when_checking_a_clean_scope : given.a_model
{
    int _exitCode;

    void Because() => _exitCode = ModelCheck.Run(["--scope", "M.F.Clean", Root, "--warnaserror"], Output, Error);

    [Fact] void should_ignore_errors_and_warnings_outside_the_scope() => _exitCode.ShouldEqual(0);
    [Fact] void should_report_non_vacuity_counts() => Output.ToString().ShouldContain("2 declaration(s), 0 direct dependent declaration(s), 0 diagnostic(s)");
    [Fact] void should_report_no_affected_scopes() => Output.ToString().ShouldContain("Affected scopes: none");
    [Fact] void should_not_treat_the_option_value_as_the_target() => Error.ToString().ShouldBeEmpty();
}
