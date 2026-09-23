// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_a_constraint;

// A file constraint is code. It stays rejected with the wording every other code attachment uses until #139 lands.
public class with_a_file_implementation : given.a_semantic_binder
{
    const string Source =
        """
        module Billing
          feature Invoices
            slice StateChange IssueInvoice
              event InvoiceIssued
                invoiceId Uuid
              constraint InvoiceStatusTransition
                file Constraints/InvoiceStatusTransitionConstraint.cs
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_fail() => _result.Success.ShouldBeFalse();
    [Fact] void should_keep_the_unsupported_semantic_syntax_code() => Rejection.Code.ShouldEqual(DiagnosticCodes.UnsupportedSemanticSyntax);
    [Fact] void should_say_it_requires_a_constrained_implementation_attachment() => Rejection.Message.ShouldEqual("Constraint 'InvoiceStatusTransition' file implementation requires a constrained implementation attachment.");

    Diagnostic Rejection => _result.Diagnostics.Single(_ => _.Message.StartsWith("Constraint 'InvoiceStatusTransition'", StringComparison.Ordinal));
}
