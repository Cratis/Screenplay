// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_billing_specifications;

public class and_the_read_model_has_an_unkeyed_list_query : given.a_semantic_binder
{
    const string Source =
        """
        module Invoicing
          feature Invoices
            slice StateView InvoiceList
              readmodel InvoiceRow
                invoiceId Uuid
                amount Decimal
              query AllInvoices => InvoiceRow[]
              specification ListingAnInvoice
                given readmodel InvoiceRow
                  invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
                  amount = 1500
                then readmodel InvoiceRow
                  invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
                  amount = 1500
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_bind_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
}
