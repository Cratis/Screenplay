// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelCheck;

public class when_checking_a_clean_scope : given.a_model
{
    int _exitCode;

    void Because() => _exitCode = ModelCheck.Run(["--scope", "M.F.Clean", Root, "--warnaserror"], Output, Error);

    [Fact] void should_keep_the_exit_code_tied_to_the_scoped_set() => _exitCode.ShouldEqual(0);
    [Fact] void should_report_non_vacuity_counts() => Output.ToString().ShouldContain("2 declaration(s), 0 direct dependent declaration(s), 0 diagnostic(s)");
    [Fact] void should_report_no_affected_scopes() => Output.ToString().ShouldContain("Affected scopes: none");
    [Fact] void should_disclose_whole_application_defects() => Output.ToString().ShouldContain("Whole application: 1 error(s), 1 warning(s) (2 outside the reported set)");
    [Fact] void should_label_the_scoped_summary() => Output.ToString().ShouldContain("0 error(s), 0 warning(s) in scope");
    [Fact] void should_not_treat_the_option_value_as_the_target() => Error.ToString().ShouldBeEmpty();
}
