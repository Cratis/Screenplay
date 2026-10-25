// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_invocation_expectations;

public class and_the_order_differs : given.invoice_closing
{
    SemanticSpecificationRun _run;

    void Establish() => CompileWith(
        """
        then invokes ArchiveInvoice
        then invokes CloseInvoice
        """);

    void Because() => _run = RunScenario();

    [Fact] void should_fail() => _run.Passed.ShouldBeFalse();
    [Fact] void should_report_each_position() => _run.Failures.Length.ShouldEqual(2);
}
