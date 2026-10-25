// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_invocation_expectations.given;

// An automation whose reaction closes, then archives, an invoice - both commands other slices own (decision 0115).
public class invoice_closing : for_SemanticSpecificationRunner.given.a_v6_scenario
{
    protected const string InvoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9";

    const string Header =
        """
        concept InvoiceId : Uuid
        module Billing
          feature Invoices
            slice StateChange CloseInvoice
              command CloseInvoice
                invoiceId InvoiceId identifier
                reason String
                produces InvoiceClosed
                  for invoiceId
                  reason = reason
              event InvoiceClosed
                reason String
            slice StateChange ArchiveInvoice
              command ArchiveInvoice
                invoiceId InvoiceId identifier
                produces InvoiceArchived
                  for invoiceId
                  invoiceId = invoiceId
              event InvoiceArchived
                invoiceId InvoiceId
            slice Automation Closing
              event InvoicePaid
                invoiceId InvoiceId
              reaction Closer
                when InvoicePaid
                  invoiceId
                  invokes CloseInvoice
                    invoiceId = invoiceId
                    reason = "paid"
                  invokes ArchiveInvoice
                    invoiceId = invoiceId
              specification Scenario
                when append InvoicePaid
                  invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"

        """;

    protected void CompileWith(string outcomes) => Compile(Header + string.Join('\n', outcomes.Split('\n').Select(line => $"        {line}")));

    protected SemanticSpecificationRun RunScenario() => Run("Scenario");
}
