// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_invocation_expectations.given;

public class an_invoice_closing_source : for_SemanticModelBinder.given.a_semantic_binder
{
    internal const string Header =
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
              specification Scenario
                when append InvoicePaid
                  invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"

        """;

    internal static string Golden => With(
        """
        then invokes CloseInvoice
          invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
          reason = "paid"
        """);

    internal static string With(string outcomes) => Header + string.Join('\n', outcomes.Split('\n').Select(line => $"        {line}"));
}
