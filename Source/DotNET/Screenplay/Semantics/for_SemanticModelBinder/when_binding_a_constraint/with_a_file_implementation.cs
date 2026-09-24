// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_a_constraint;

// A file constraint cannot express anything beyond uniqueness in Chronicle, and ESM cannot admit the attachment.
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
    [Fact] void should_explain_the_portable_alternative() => Rejection.Message.ShouldEqual("Constraint 'InvoiceStatusTransition' file implementation is not admitted by the executable model: Chronicle file constraints can only declare uniqueness. Declare it with 'unique ...' for portability; put other rules in command validation or a 'require' condition.");

    [Fact] void should_list_the_constraint_file() => _result.ImplementationRequirements.Single().File.ShouldEqual("Constraints/InvoiceStatusTransitionConstraint.cs");
    [Fact] void should_type_the_constraint_role() => _result.ImplementationRequirements.Single().Role.ShouldEqual(SemanticImplementationRole.ConstraintPredicate);

    Diagnostic Rejection => _result.Diagnostics.Single(_ => _.Message.StartsWith("Constraint 'InvoiceStatusTransition'", StringComparison.Ordinal));
}
