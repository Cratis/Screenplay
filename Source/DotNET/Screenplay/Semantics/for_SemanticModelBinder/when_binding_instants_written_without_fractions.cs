// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_instants_written_without_fractions : given.a_semantic_binder
{
    const string Source =
        """
        module Billing
          feature Invoicing
            slice StateChange SendInvoice
              command SendInvoice
                invoiceId Uuid identifier
                sentAt DateTime
                produces InvoiceSent
                  for invoiceId
                  sentAt = sentAt
              event InvoiceSent
                sentAt DateTime
              specification SendingAnInvoice
                when SendInvoice
                  invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
                  sentAt = "2026-10-05T08:00:00Z"
                then InvoiceSent
                  for "9c858901-8a57-4791-81fe-4c455b099bc9"
                  sentAt = "2026-10-05T10:00:00.0000000+02:00"
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_bind_the_short_form_to_the_round_trip_form() => Specification.When!.Values[1].Value.ShouldEqual(SemanticValue.Text("2026-10-05T08:00:00.0000000Z"));
    [Fact] void should_keep_a_round_trip_value_as_written() => Specification.ThenEvents.Single().Values.Single().Value.ShouldEqual(SemanticValue.Text("2026-10-05T10:00:00.0000000+02:00"));

    SemanticSpecification Specification => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single();
}
