// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Observations.for_ObservationComparison.given;

namespace Cratis.Screenplay.Observations.for_ObservationSnapshot.when_creating;

public class with_duplicate_entries : Specification
{
    [Theory]
    [InlineData("registered")]
    [InlineData("appends")]
    [InlineData("edges")]
    [InlineData("unattributed")]
    [InlineData("consumers")]
    [InlineData("subscriptions")]
    void should_refuse_duplicate_keys(string duplicate)
    {
        var type = new ObservedEventType("Recorded", 1);
        var producer = new ObservedProducer(ObservedProducerKind.Command, "Record");
        var consumer = new ObservedConsumer("reaction", ObservedConsumerKind.Reaction, ObservedConsumerOwner.Application, "log", duplicate == "subscriptions" ? [type, type] : [type]);
        var builder = new snapshot_builder
        {
            Registered = duplicate == "registered" ? [type, type] : [type],
            Appends = duplicate == "appends" ? [new(type, 4), new(type, 4)] : [new(type, 4)],
            Edges = duplicate == "edges" ? [new(producer, type, 1), new(producer, type, 1)] : [],
            Unattributed = duplicate == "unattributed" ? [new(type, 1), new(type, 1)] : [],
            Consumers = duplicate == "consumers" ? [consumer, consumer] : [consumer]
        };
        Catch.Exception(() => builder.Build()).ShouldBeOfExactType<InvalidObservationSnapshot>();
    }
}
