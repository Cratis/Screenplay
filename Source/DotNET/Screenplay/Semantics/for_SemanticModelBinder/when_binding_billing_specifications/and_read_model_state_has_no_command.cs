// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_billing_specifications;

public class and_read_model_state_has_no_command : given.a_semantic_binder
{
    const string Source =
        """
        concept InvoiceId : Uuid
        module Invoicing
          feature Invoices
            slice StateView InvoiceLookup
              readmodel InvoiceSummary
                invoiceId InvoiceId
                amount Decimal
                status String?
              query InvoiceById => InvoiceSummary?
                by invoiceId InvoiceId
              specification LookingUpAnInvoice
                given readmodel InvoiceSummary
                  invoiceId = "8f14e45f-ceea-467a-9c2b-1b7f2ec2a1c1"
                  amount = 1500
                  status = null
                then readmodel InvoiceSummary
                  invoiceId = "8f14e45f-ceea-467a-9c2b-1b7f2ec2a1c1"
                  amount = 1500
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_bind_the_whenless_read_model_assertion() => _result.Success.ShouldBeTrue();
    [Fact] void should_preserve_the_absent_command() =>
        _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single().When.ShouldBeNull();
}
