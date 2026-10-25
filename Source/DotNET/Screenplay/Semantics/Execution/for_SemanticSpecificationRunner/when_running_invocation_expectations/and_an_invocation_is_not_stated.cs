// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_invocation_expectations;

public class and_an_invocation_is_not_stated : given.invoice_closing
{
    SemanticSpecificationRun _run;

    void Establish() => CompileWith(
        """
        then invokes CloseInvoice
          reason = "paid"
        """);

    void Because() => _run = RunScenario();

    [Fact] void should_fail() => _run.Passed.ShouldBeFalse();
    [Fact] void should_report_the_count_and_list_both_sides() => _run.Failures.ShouldContainOnly(["Expected 1 invocation(s), got 2. Expected: CloseInvoice(reason = \"paid\"). Actual: CloseInvoice(invoiceId = \"9c858901-8a57-4791-81fe-4c455b099bc9\", reason = \"paid\"), ArchiveInvoice(invoiceId = \"9c858901-8a57-4791-81fe-4c455b099bc9\")."]);
}
