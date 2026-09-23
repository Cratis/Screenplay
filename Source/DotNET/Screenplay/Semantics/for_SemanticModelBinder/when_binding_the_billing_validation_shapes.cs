// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

// The validation shapes of Stage's Billing sample (RegisterInvoice.play and application.play) bind to ESM v1.
public class when_binding_the_billing_validation_shapes : given.a_semantic_binder
{
    const string Source =
        """
        concept InvoiceId : Uuid
        concept InvoiceNumber : String
          validate
            not empty message "An invoice needs a number"
            length == 10
        concept Money : Decimal
        module Invoicing
          feature Invoices
            slice StateChange RegisterInvoice
              command RegisterInvoice
                invoiceId InvoiceId identifier
                invoiceNumber InvoiceNumber
                amount Money
                validate
                  invoiceNumber not empty message "Invoice number is required"
                  amount > 0 message "An invoice for nothing is not an invoice"
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_bind_the_amount_rule() => Command.Validations[1].Kind.ShouldEqual(SemanticValidationRuleKind.GreaterThan);
    [Fact] void should_bind_the_amount_operand() => Command.Validations[1].Operand.ShouldEqual(SemanticValue.Number(0));
    [Fact] void should_bind_the_amount_message() => Command.Validations[1].Message.ShouldEqual("An invoice for nothing is not an invoice");
    [Fact] void should_bind_the_invoice_number_length() => InvoiceNumber.Validations[1].Kind.ShouldEqual(SemanticValidationRuleKind.Length);
    [Fact] void should_bind_the_invoice_number_length_operand() => InvoiceNumber.Validations[1].Operand.ShouldEqual(SemanticValue.Number(10));
    [Fact] void should_bind_the_length_without_a_message() => InvoiceNumber.Validations[1].Message.ShouldBeNull();

    SemanticCommand Command => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single();
    SemanticConcept InvoiceNumber => _result.Value!.Model.Application.Concepts.Single(_ => _.Name == "InvoiceNumber");
}
