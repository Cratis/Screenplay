// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_reactions;

public class and_a_command_sets_off_a_cascade : given.a_v6_scenario
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
                then InvoiceWelcomed
                  for "9c858901-8a57-4791-81fe-4c455b099bc9"
                  invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
                  welcomedAt = "2026-10-02T09:00:00Z"
                then InvoiceClosed
                  for "9c858901-8a57-4791-81fe-4c455b099bc9"
                  reason = "welcomed"
              specification ForgettingTheReactions
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
        """;

    SemanticSpecificationRun _cascade;
    SemanticSpecificationRun _forgotten;

    void Establish() => Compile(Source);

    void Because()
    {
        _cascade = Run("RegisteringAnInvoice");
        _forgotten = Run("ForgettingTheReactions");
    }

    [Fact] void should_select_esm_v6() => _plan.Model.SemanticVersion.ShouldEqual(SemanticVersion.V6);
    [Fact] void should_pass_with_what_the_reactions_appended() => _cascade.Passed.ShouldBeTrue();
    [Fact] void should_append_the_command_the_reaction_and_the_invoked_command_facts() => ((SemanticAccepted)_cascade.Execution).Facts.Length.ShouldEqual(3);
    [Fact] void should_fail_a_specification_that_leaves_the_reactions_out() => _forgotten.Failures.ShouldContain("Expected 1 fact(s), got 3.");
}
