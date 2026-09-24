// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_billing_specifications;

public class and_an_event_value_is_null : given.a_semantic_binder
{
    const string Source =
        """
        module Invoicing
          feature Invoices
            slice StateChange RegisterInvoice
              command RegisterInvoice
                invoiceNumber String
                produces InvoiceRegistered
                  invoiceNumber = invoiceNumber
              event InvoiceRegistered
                invoiceNumber String?
              specification NullEventValue
                when RegisterInvoice
                  invoiceNumber = "INV-000123"
                then InvoiceRegistered
                  invoiceNumber = null
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_report_a_precise_diagnostic_instead_of_throwing() => _result.Diagnostics.Any(_ => _.Code == DiagnosticCodes.NullSpecificationFact && _.Message.Contains("an optional fact is a separate event", StringComparison.Ordinal)).ShouldBeTrue();
}
