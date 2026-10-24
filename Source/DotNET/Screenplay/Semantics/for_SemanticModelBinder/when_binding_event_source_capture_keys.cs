// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_event_source_capture_keys : given.a_semantic_binder
{
    internal const string Source = """
        concept OrderId : Uuid
        module Sales
          feature Orders
            slice Translate ReceiveOrders
              direction inbound
              event OrderPlaced from "storefront"
                orderId OrderId
              event OrderReceived
                orderId OrderId
              capture OrderIntake
                source events
                  from OrderPlaced
                key orderId is event source
                append OrderReceived
                  orderId = $.orderId
              specification AReceivedOrderIsSourcedByItsKey
                given OrderReceived
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  orderId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                then readmodel OrderStateEntry
                  orderId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
            slice StateView OrderState
              readmodel OrderStateEntry
                orderId OrderId key
              projection OrderStateProjection => OrderStateEntry
                from OrderReceived
                  key $eventSourceId
                  orderId = $eventSourceId
        """;

    [Fact]
    void should_flag_the_capture_select_v33_and_round_trip()
    {
        var result = Bind(Source);
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(value => value.Message)));
        var model = result.Value!.Model;
        model.SemanticVersion.Major.ShouldEqual(33u);
        model.Application.Modules.Single().Features.Single().Slices[0].Captures.Single().EventsSource!.KeyIsEventSource.ShouldBeTrue();
        var bytes = SemanticModelSerializer.Serialize(model);
        SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(bytes)).ShouldEqual(bytes);
    }

    [Fact]
    void should_leave_an_unmarked_capture_on_its_earlier_version_without_the_member()
    {
        var result = Bind(Source.Replace(" is event source", string.Empty, StringComparison.Ordinal));
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(value => value.Message)));
        result.Value!.Model.SemanticVersion.Major.ShouldEqual(32u);
        System.Text.Encoding.UTF8.GetString(SemanticModelSerializer.Serialize(result.Value.Model)).Contains("keyIsEventSource", StringComparison.Ordinal).ShouldBeFalse();
    }

    [Fact]
    void should_reject_the_flag_under_an_earlier_schema()
    {
        var model = Bind(Source).Value!.Model;
        Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V32, SemanticVersion.V32, model.Application))
            .ShouldBeOfExactType<InvalidSemanticContract>();
        EsmSchemaV32Support.Supports(LanguageVersion.V33, SemanticVersion.V33).ShouldBeFalse();
    }
}
