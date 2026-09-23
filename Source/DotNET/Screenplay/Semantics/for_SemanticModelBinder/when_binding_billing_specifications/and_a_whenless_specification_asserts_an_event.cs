// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_billing_specifications;

public class and_a_whenless_specification_asserts_an_event : given.a_semantic_binder
{
    const string Source =
        """
        module Invoicing
          feature Invoices
            slice StateView InvoiceLookup
              readmodel InvoiceSummary
                invoiceId Uuid
              query InvoiceById => InvoiceSummary?
                by invoiceId Uuid
              event InvoiceRegistered
              specification InvalidReadOnlyAssertion
                then InvoiceRegistered
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_reject_events_without_a_command() => _result.Diagnostics.Any(_ => _.Code == DiagnosticCodes.InvalidWhenlessSpecification).ShouldBeTrue();
}
