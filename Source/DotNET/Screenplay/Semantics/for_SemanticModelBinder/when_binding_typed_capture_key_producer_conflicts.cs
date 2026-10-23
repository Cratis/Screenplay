// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_typed_capture_key_producer_conflicts : given.a_semantic_binder
{
    [Fact]
    void should_preserve_text_and_the_legacy_version_when_a_command_producer_has_no_identifier()
    {
        var source = when_binding_typed_capture_keys.Source[..when_binding_typed_capture_keys.Source.IndexOf("      specification", StringComparison.Ordinal)] + """

              command ReceiveOrder
                produces OrderReceived
                  orderId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        """;
        var result = Bind(source);
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(value => value.Message)));
        var model = result.Value!.Model;
        model.SemanticVersion.Major.ShouldEqual(9u);
        model.Application.Modules.Single().Features.Single().Slices.Single().Captures.Single().Appends.Single()
            .EventSourceType.ShouldEqual(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text));
    }

    [Fact]
    void should_consider_an_upstream_capture_identity_when_a_reaction_inherits_it()
    {
        var source = when_binding_typed_capture_keys.Source[..when_binding_typed_capture_keys.Source.IndexOf("      specification", StringComparison.Ordinal)] + """

              event CustomerPlaced from "customers"
                customerId CustomerId
              event CustomerReceived
                customerId CustomerId
              capture CustomerIntake
                source events
                  from CustomerPlaced
                key customerId
                append CustomerReceived
                  customerId = $.customerId
            slice Automation OtherProducer
              reaction ProduceOrder
                when CustomerReceived
                  produces OrderReceived
                    orderId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        concept CustomerId : Uuid
        """;
        var result = Bind(source);
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(value => value.Message)));
        var model = result.Value!.Model;
        model.SemanticVersion.Major.ShouldEqual(32u);
        var captures = model.Application.Modules.Single().Features.Single().Slices.Single(slice => !slice.Captures.IsEmpty).Captures;
        captures[0].Appends.Single().EventSourceType.ShouldEqual(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text));
        captures[1].Appends.Single().EventSourceType.ShouldEqual(SemanticTypeReference.ForConcept(model.Application.Concepts.Single(concept => concept.Name == "CustomerId").Id));
    }
}
