// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_billing_specifications;

public class and_the_read_model_has_no_keyed_query : given.a_semantic_binder
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
                  amount = 1500
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_fail() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_read_model_it_cannot_identify() =>
        _result.Diagnostics.Count(_ => _.Message.Contains("Read model 'InvoiceRow' must have one unambiguous keyed query", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_not_ask_the_blocks_for_an_identifier_nobody_can_name() =>
        _result.Diagnostics.Any(_ => _.Code == DiagnosticCodes.MissingSpecificationReadModelIdentifier).ShouldBeFalse();
}
