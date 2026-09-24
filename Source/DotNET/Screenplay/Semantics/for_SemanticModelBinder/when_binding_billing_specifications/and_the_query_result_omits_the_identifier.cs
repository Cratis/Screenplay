// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_billing_specifications;

public class and_the_query_result_omits_the_identifier : given.a_semantic_binder
{
    const string Source =
        """
        concept InvoiceId : Uuid
        module Invoicing
          feature Invoices
            slice StateChange RegisterInvoice
              command RegisterInvoice
                invoiceId InvoiceId identifier
                invoiceNumber String
                amount Decimal
                produces InvoiceRegistered
                  for invoiceId
                  invoiceId = invoiceId
                  invoiceNumber = invoiceNumber
                  amount = amount
              event InvoiceRegistered
                invoiceId InvoiceId
                invoiceNumber String
                amount Decimal
              specification RegisteringAnInvoice
                when RegisterInvoice
                  invoiceId = "8f14e45f-ceea-467a-9c2b-1b7f2ec2a1c1"
                  invoiceNumber = "INV-000123"
                  amount = 1500
                then query InvoiceById
                  arguments
                    invoiceId = "8f14e45f-ceea-467a-9c2b-1b7f2ec2a1c1"
                  result
                    invoiceNumber = "INV-000123"
                    amount = 1500
            slice StateView InvoiceLookup
              readmodel InvoiceSummary
                invoiceId InvoiceId
                invoiceNumber String
                amount Decimal
                status String
              query InvoiceById => InvoiceSummary?
                by invoiceId InvoiceId
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_bind_the_subset_result() => _result.Success.ShouldBeTrue();
    [Fact] void should_derive_the_result_key_from_the_query_argument() =>
        _result.Value!.Model.Application.Modules.Single().Features.Single().Slices
            .SelectMany(_ => _.Specifications).Single().ThenQueries.Single().Results.Single().Values.Length.ShouldEqual(2);
}
