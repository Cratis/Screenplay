// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_rejecting_unadmitted_command_guards : given.a_semantic_binder
{
    const string Source =
        """
        module Billing
          feature Invoicing
            slice StateChange IssueInvoice
              command IssueInvoice
                invoiceId Uuid identifier
                amount Decimal
                validate
                  require InvoiceState.status == "draft"
                    message "Only drafts can be issued"
                produces when amount > $env.MINIMUM
                  InvoiceIssued
                    tag $context.identity.id
                    invoiceId = invoiceId
              event InvoiceIssued
                invoiceId Uuid
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_reject_reads_until_decision_consistency_is_defined() => _result.Diagnostics.Any(_ => _.Message.Contains("#129", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_reject_non_deterministic_environment_values() => _result.Diagnostics.Any(_ => _.Message.Contains("$env", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_reject_context_tags_until_v2() => _result.Diagnostics.Any(_ => _.Message.Contains("#226", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_point_to_the_tag_line() => _result.Diagnostics.Single(_ => _.Message.Contains("Tag is not admitted", StringComparison.Ordinal)).Location.Line.ShouldEqual(12);
}
