// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_invocation_expectations;

public class and_the_order_differs_in_any_order : given.invoice_closing
{
    SemanticSpecificationRun _run;

    void Establish() => CompileWith(
        """
        then events in any order
        then invokes ArchiveInvoice
        then invokes CloseInvoice
          reason = "paid"
        """);

    void Because() => _run = RunScenario();

    [Fact] void should_match_them_as_a_multiset() => Assert.True(_run.Passed, string.Join('\n', _run.Failures));
}
