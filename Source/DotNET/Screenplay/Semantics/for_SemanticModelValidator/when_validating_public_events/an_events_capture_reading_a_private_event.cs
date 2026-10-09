// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelValidator.when_validating_public_events;

public class an_events_capture_reading_a_private_event : given.a_model_to_corrupt
{
    Exception _error;

    void Because() => _error = Rebuild(InboundModel, "TrackShipments", slice => slice with { Captures = [.. slice.Captures.Select(capture => capture with { EventsSource = new SemanticCaptureEventsSource([slice.Events.Single(@event => @event.Visibility == SemanticEventVisibility.Private).Id]) })] });

    [Fact] void should_refuse_the_model() => _error.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_say_why() => _error.Message.ShouldContain("declared public events with an origin");
}
