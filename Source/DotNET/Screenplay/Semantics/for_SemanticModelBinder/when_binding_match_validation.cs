// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_match_validation : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _email;
    CompilationResult<SemanticCompilation> _regex;
    CompilationResult<SemanticCompilation> _unknown;
    CompilationResult<SemanticCompilation> _invalid;
    CompilationResult<SemanticCompilation> _number;

    void Because()
    {
        _email = Bind("""
            concept EmailAddress : String
              validate
                matches email message "Invalid address"
            """);
        _regex = Bind(Source("String", "matches \"INV-\""));
        _unknown = Bind(Source("String", "matches phone"));
        _invalid = Bind(Source("String", "matches \"[\""));
        _number = Bind(Source("Int", "matches email"));
    }

    static string Source(string type, string rule) => $"""
        module Billing
          feature Invoices
            slice StateChange RegisterInvoice
              command RegisterInvoice
                invoiceNumber {type}
                validate
                  invoiceNumber {rule}
        """;

    [Fact] void should_bind_the_billing_email_address_without_pii() => _email.Success.ShouldBeTrue();
    [Fact] void should_expand_email_to_a_portable_pattern() => _email.Value!.Model.Application.Concepts.Single().Validations.Single().Operand.ShouldEqual(SemanticValue.Text(SemanticMatchPattern.Email));
    [Fact] void should_preserve_a_quoted_pattern() => _regex.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single().Validations.Single().Operand.ShouldEqual(SemanticValue.Text("INV-"));
    [Fact] void should_reject_an_unknown_named_pattern() => _unknown.Diagnostics.Any(_ => _.Code == DiagnosticCodes.UnknownMatchPattern && _.Message.Contains("phone", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_report_an_invalid_pattern_at_the_rule() => _invalid.Diagnostics.Any(_ => _.Code == DiagnosticCodes.InvalidMatchPattern && _.Location.Line == 7).ShouldBeTrue();
    [Fact] void should_reject_a_non_text_subject() => _number.Diagnostics.Any(_ => _.Code == DiagnosticCodes.UnsupportedSemanticSyntax && _.Message.Contains("text value", StringComparison.Ordinal)).ShouldBeTrue();
}
