// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelValidator.when_validating_public_events;

public class a_public_event_with_a_blank_origin : given.a_model_to_corrupt
{
    Exception _error;

    void Because() => _error = Rebuild(InboundModel, "TrackShipments", slice => slice with { Events = [.. slice.Events.Select(@event => @event.Origin is null ? @event : @event with { Origin = " " })] });

    [Fact] void should_refuse_the_model() => _error.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_say_why() => _error.Message.ShouldContain("origin must be a nonblank store name");
}
