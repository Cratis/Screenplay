// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_invocation_expectations;

public class and_the_invocations_match : given.invoice_closing
{
    SemanticSpecificationRun _run;

    void Establish() => CompileWith(
        """
        then invokes CloseInvoice
          invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
          reason = "paid"
        then invokes ArchiveInvoice
          invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
        """);

    void Because() => _run = RunScenario();

    [Fact] void should_select_esm_v34() => _plan.Model.SemanticVersion.ShouldEqual(SemanticVersion.V34);
    [Fact] void should_pass() => Assert.True(_run.Passed, string.Join('\n', _run.Failures));
    [Fact] void should_record_the_invocations_in_order() => _run.Invocations.Select(invocation => _plan.Commands[invocation.Command].Name).ShouldEqual(["CloseInvoice", "ArchiveInvoice"]);
    [Fact] void should_record_that_the_commands_accepted() => _run.Invocations.All(invocation => invocation.Accepted).ShouldBeTrue();
    [Fact] void should_record_the_values_sent() => _run.Invocations[0].Values.Select(value => value.Value).ShouldContain(SemanticValue.Text("paid"));
    [Fact] void should_not_adopt_the_facts_of_the_commands_other_slices_own() => ((SemanticAccepted)_run.Execution).Facts.Length.ShouldEqual(1);
}
