// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_enumeration_members_written_bare : given.a_semantic_binder
{
    const string Source =
        """
        concept InvoiceStatus : Enum
          draft
          sent
        module Billing
          feature Invoicing
            slice StateChange ChangeStatus
              command ChangeStatus
                invoiceId Uuid identifier
                status InvoiceStatus
                produces when status == sent
                  InvoiceSent
                    for invoiceId
                    status = status
              event InvoiceSent
                status InvoiceStatus
              specification SendingAnInvoice
                when ChangeStatus
                  invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
                  status = sent
                then InvoiceSent
                  for "9c858901-8a57-4791-81fe-4c455b099bc9"
                  status = InvoiceStatus.sent
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_compare_with_the_member() => ((SemanticComparison)Slice.Commands.Single().Produces.Single().When!).Right.Value.ShouldEqual(SemanticValue.Text("sent"));
    [Fact] void should_bind_a_bare_member_in_a_command_value() => Specification.When!.Values[1].Value.ShouldEqual(SemanticValue.Text("sent"));
    [Fact] void should_bind_a_qualified_member_in_an_event_value() => Specification.ThenEvents.Single().Values.Single().Value.ShouldEqual(SemanticValue.Text("sent"));

    SemanticSpecification Specification => Slice.Specifications.Single();
    SemanticSlice Slice => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single();
}
