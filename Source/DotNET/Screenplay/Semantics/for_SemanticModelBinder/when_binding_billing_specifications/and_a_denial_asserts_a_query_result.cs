// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_billing_specifications;

public class and_a_denial_asserts_a_query_result : given.a_semantic_binder
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
              specification DenyingAnInvoiceLookup
                then query InvoiceById
                  arguments
                    invoiceId = "00000000-0000-0000-0000-000000000001"
                  result
                    invoiceId = "00000000-0000-0000-0000-000000000001"
                then denied
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_refuse_binding() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_missing_action_before_the_conflicting_outcome() =>
        _result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Select(diagnostic => diagnostic.Code)
            .ShouldEqual(DiagnosticCodes.InvalidWhenlessSpecification, DiagnosticCodes.InvalidSpecificationDenied);
}
