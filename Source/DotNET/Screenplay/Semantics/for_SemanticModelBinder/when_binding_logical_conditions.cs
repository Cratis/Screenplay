// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_logical_conditions : given.a_semantic_binder
{
    const string Source =
        """
        module Billing
          feature Invoicing
            slice StateChange IssueInvoice
              command IssueInvoice
                invoiceId Uuid identifier
                isProForma Bool
                amount Decimal
                produces when isProForma == true or amount > 0 and amount < 100
                  InvoiceIssued
                    for invoiceId
                    invoiceId = invoiceId
              event InvoiceIssued
                invoiceId Uuid
        """;

    SemanticCondition _condition;

    void Because() => _condition = Bind(Source).Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single().Produces.Single().When!;

    [Fact] void should_preserve_or_at_the_root() => ((SemanticLogicalCondition)_condition).Operator.ShouldEqual(SemanticLogicalOperator.Or);
    [Fact] void should_bind_and_more_tightly_than_or() => ((SemanticLogicalCondition)((SemanticLogicalCondition)_condition).Right).Operator.ShouldEqual(SemanticLogicalOperator.And);
}
