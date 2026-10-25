// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_invocation_expectations;

public class and_only_some_values_are_stated : given.invoice_closing
{
    SemanticSpecificationRun _run;

    void Establish() => CompileWith(
        """
        then invokes CloseInvoice
          reason = "paid"
        then invokes ArchiveInvoice
        """);

    void Because() => _run = RunScenario();

    [Fact] void should_not_compare_the_unstated_values() => Assert.True(_run.Passed, string.Join('\n', _run.Failures));
}
