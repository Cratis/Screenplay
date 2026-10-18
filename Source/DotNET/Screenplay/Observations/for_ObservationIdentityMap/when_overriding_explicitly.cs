// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations.for_ObservationIdentityMap;

public class when_overriding_explicitly : for_ObservationComparison.given.a_compiled_model
{
    ObservationIdentityMap _overridden = null!;

    void Establish() => Model(ReactionSource);
    void Because() => _overridden = _map.With(Slice.Reactions[0].Id, producer: new(ObservedProducerKind.Reaction, "host.reactor"), consumerIdentifier: "reactor-id")
        .With(Slice.Events[0].Id, eventTypeId: "host.event");

    [Fact] void should_use_the_host_producer() => _overridden.Producers[Slice.Reactions[0].Id].Identifier.ShouldEqual("host.reactor");
    [Fact] void should_use_the_host_consumer() => _overridden.Consumers[Slice.Reactions[0].Id].ShouldEqual("reactor-id");
    [Fact] void should_use_the_host_event_type() => _overridden.EventTypes[Slice.Events[0].Id].ShouldEqual("host.event");
    [Fact] void should_leave_the_original_map_unchanged() => _map.Producers.ShouldBeEmpty();
}
