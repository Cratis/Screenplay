// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_reactions;

// A specification runs only its own slice: the command's specification sees none of the reactions, and the automation's
// sees its own reactions' facts but not those of the command another slice owns.
public class and_other_slices_react_to_the_command : given.a_v6_scenario
{
    const string Source =
        """
        concept InvoiceId : Uuid
        module Billing
          feature Invoices
            slice StateChange RegisterInvoice
              command RegisterInvoice
                invoiceId InvoiceId identifier
                number String
                produces InvoiceRegistered
                  for invoiceId
                  invoiceId = invoiceId
                  number = number
                  registeredAt = $context.occurred
              event InvoiceRegistered
                invoiceId InvoiceId
                number String
                registeredAt DateTime
              specification RegisteringAnInvoice
                given clock "2026-10-02T09:00:00Z"
                when RegisterInvoice
                  invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
                  number = "INV-1"
                then InvoiceRegistered
                  for "9c858901-8a57-4791-81fe-4c455b099bc9"
                  invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
                  number = "INV-1"
                  registeredAt = "2026-10-02T09:00:00Z"
            slice StateChange CloseInvoice
              command CloseInvoice
                invoiceId InvoiceId identifier
                reason String
                produces InvoiceClosed
                  for invoiceId
                  reason = reason
              event InvoiceClosed
                reason String
            slice Automation Welcome
              reaction Welcomer
                when InvoiceRegistered
                  invoiceId
                  produces InvoiceWelcomed
                    invoiceId = invoiceId
                    welcomedAt = $context.occurred
              reaction Closer
                when InvoiceWelcomed
                  invoiceId
                  invokes CloseInvoice
                    invoiceId = invoiceId
                    reason = "welcomed"
              event InvoiceWelcomed
                invoiceId InvoiceId
                welcomedAt DateTime
              specification WelcomingAnInvoice
                given clock "2026-10-02T09:00:00Z"
                when append InvoiceRegistered
                  for "9c858901-8a57-4791-81fe-4c455b099bc9"
                  invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
                  number = "INV-1"
                  registeredAt = "2026-10-02T09:00:00Z"
                then InvoiceWelcomed
                  for "9c858901-8a57-4791-81fe-4c455b099bc9"
                  invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
                  welcomedAt = "2026-10-02T09:00:00Z"
        """;

    SemanticSpecificationRun _command;
    SemanticSpecificationRun _automation;

    void Establish() => Compile(Source);

    void Because()
    {
        _command = Run("RegisteringAnInvoice");
        _automation = Run("WelcomingAnInvoice");
    }

    [Fact] void should_select_esm_v6() => _plan.Model.SemanticVersion.ShouldEqual(SemanticVersion.V6);
    [Fact] void should_pass_the_command_with_its_own_fact() => Assert.True(_command.Passed, string.Join('\n', _command.Failures));
    [Fact] void should_not_run_other_slices_reactions_for_the_command() => ((SemanticAccepted)_command.Execution).Facts.Length.ShouldEqual(1);
    [Fact] void should_pass_the_automation_with_its_own_reactions_facts() => Assert.True(_automation.Passed, string.Join('\n', _automation.Failures));
    [Fact] void should_not_adopt_the_facts_of_the_command_another_slice_owns() => ((SemanticAccepted)_automation.Execution).Facts.Select(_ => _plan.Events[_.EventContract].Name).ShouldEqual(["InvoiceRegistered", "InvoiceWelcomed"]);
}
