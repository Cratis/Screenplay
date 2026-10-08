// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_DataBindingCompleteness;

public class when_the_check_is_not_selected : given.a_screen
{
    void Establish() => Compile("data R[] via query Q\ndata R via query One");
    void Because() => Findings = ModelCompleteness.Check(Compilation, CompletenessChecks.None);

    [Fact] void should_not_report_completeness_warnings() => Findings.ShouldBeEmpty();
    [Fact] void should_leave_compiler_diagnostics_unchanged() => Compilation.Diagnostics.ShouldBeEmpty();
}
