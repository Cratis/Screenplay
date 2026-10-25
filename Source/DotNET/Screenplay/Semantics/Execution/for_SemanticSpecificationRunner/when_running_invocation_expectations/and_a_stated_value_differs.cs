// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_invocation_expectations;

public class and_a_stated_value_differs : given.invoice_closing
{
    SemanticSpecificationRun _run;

    void Establish() => CompileWith(
        """
        then invokes CloseInvoice
          reason = "cancelled"
        then invokes ArchiveInvoice
        """);

    void Because() => _run = RunScenario();

    [Fact] void should_fail() => _run.Passed.ShouldBeFalse();
    [Fact] void should_name_the_invocation_and_both_values() => _run.Failures.ShouldContainOnly(["Invocation at index 0 does not match the expected command and values. Expected: CloseInvoice(reason = \"cancelled\"). Actual: CloseInvoice(invoiceId = \"9c858901-8a57-4791-81fe-4c455b099bc9\", reason = \"paid\")."]);
}
