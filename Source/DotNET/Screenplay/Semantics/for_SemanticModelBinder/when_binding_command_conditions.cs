// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_command_conditions : given.a_semantic_binder
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
                validate
                  require amount > 0
                    message "An invoice for nothing is not an invoice"
                produces when isProForma == true
                  InvoiceRegistered
                    for invoiceId
                    tag billing
                    invoiceId = invoiceId
              event InvoiceRegistered
                tag invoicing
                invoiceId Uuid
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_bind_one_requirement() => Command.Requirements.Length.ShouldEqual(1);
    [Fact] void should_preserve_the_requirement_message() => Command.Requirements.Single().Message.ShouldEqual("An invoice for nothing is not an invoice");
    [Fact] void should_bind_the_production_guard() => (Command.Produces.Single().When is SemanticComparison).ShouldBeTrue();
    [Fact] void should_bind_append_tags() => Command.Produces.Single().Tags.Single().ShouldEqual("billing");
    [Fact] void should_bind_event_tags() => Slice.Events.Single().Tags.Single().ShouldEqual("invoicing");

    SemanticCommand Command => Slice.Commands.Single();
    SemanticSlice Slice => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single();
}
