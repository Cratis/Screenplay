// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_rejecting_nonportable_conditions : given.a_semantic_binder
{
    const string Source =
        """
        module Billing
          feature Invoicing
            slice StateChange IssueInvoice
              command IssueInvoice
                invoiceId Uuid identifier
                produces when amount > 0
                  InvoiceIssued
                    invoiceId = invoiceId
              event InvoiceIssued
                invoiceId Uuid
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_reject_a_read_dependent_amount() => _result.Success.ShouldBeFalse();
    [Fact] void should_name_the_missing_command_property_and_reads_issue() =>
        _result.Diagnostics.Any(_ => _.Message.Contains("amount", StringComparison.Ordinal) && _.Message.Contains("#129", StringComparison.Ordinal)).ShouldBeTrue();
}
