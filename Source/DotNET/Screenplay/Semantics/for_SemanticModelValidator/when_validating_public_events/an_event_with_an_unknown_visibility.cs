// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelValidator.when_validating_public_events;

public class an_event_with_an_unknown_visibility : given.a_model_to_corrupt
{
    Exception _error;

    void Because() => _error = Rebuild(OutboundModel, "PublishOrderShipped", slice => slice with { Events = [.. slice.Events.Select(@event => @event with { Visibility = (SemanticEventVisibility)99 })] });

    [Fact] void should_refuse_the_model() => _error.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_say_why() => _error.Message.ShouldContain("unknown visibility");
}
