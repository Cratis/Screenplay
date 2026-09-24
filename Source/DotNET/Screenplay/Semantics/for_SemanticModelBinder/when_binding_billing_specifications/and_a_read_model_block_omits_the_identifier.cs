// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_billing_specifications;

public class and_a_read_model_block_omits_the_identifier : given.a_semantic_binder
{
    const string Source =
        """
        module Invoicing
          feature Invoices
            slice StateView InvoiceLookup
              readmodel InvoiceSummary
                invoiceId Uuid
                amount Decimal
              query InvoiceById => InvoiceSummary?
                by invoiceId Uuid
              specification LookingUpAnInvoice
                given readmodel InvoiceSummary
                  amount = 1500
                then readmodel InvoiceSummary
                  amount = 1500
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_report_each_block_at_its_own_location() =>
        _result.Diagnostics.Where(_ => _.Code == DiagnosticCodes.MissingSpecificationReadModelIdentifier)
            .Select(_ => _.Location.Line).ShouldEqual(10, 12);
}
