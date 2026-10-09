// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_public_events;

public class an_inbound_translation : given.a_public_events_model
{
    CompilationResult<SemanticCompilation> _result;
    SemanticSlice _slice;
    ExecutableSemanticModel _roundTripped;

    void Because()
    {
        _result = Bind(Inbound);
        Assert.True(_result.Success, Messages(_result));
        _slice = Slice(_result, "TrackShipments");
        _roundTripped = SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(_result.Value!.Model));
    }

    [Fact] void should_claim_esm_v9() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V9);
    [Fact] void should_bind_the_origin() => _slice.Events.Single(@event => @event.Name == "ShipmentDispatched").Origin.ShouldEqual("shipping");
    [Fact] void should_mark_the_foreign_event_public() => _slice.Events.Single(@event => @event.Name == "ShipmentDispatched").Visibility.ShouldEqual(SemanticEventVisibility.Public);
    [Fact] void should_keep_the_local_event_private() => _slice.Events.Single(@event => @event.Name == "OrderDispatched").Visibility.ShouldEqual(SemanticEventVisibility.Private);
    [Fact] void should_read_the_consumed_event() => _slice.Captures.Single().EventsSource!.Events.Single().ShouldEqual(_slice.Events.Single(@event => @event.Name == "ShipmentDispatched").Id);
    [Fact] void should_round_trip_canonically() => _roundTripped.Revision.ShouldEqual(_result.Value!.Model.Revision);
}
