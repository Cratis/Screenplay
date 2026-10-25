// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_invocation_expectations;

public class and_a_stated_invocation_is_not_made : given.invoice_closing
{
    SemanticSpecificationRun _run;

    void Establish() => CompileWith(
        """
        then invokes CloseInvoice
        then invokes ArchiveInvoice
        then invokes CloseInvoice
        """);

    void Because() => _run = RunScenario();

    [Fact] void should_fail() => _run.Passed.ShouldBeFalse();
    [Fact] void should_report_the_count() => _run.Failures.Single().ShouldContain("Expected 3 invocation(s), got 2.");
}
