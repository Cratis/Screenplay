// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Observations.for_ObservationComparison.given;

namespace Cratis.Screenplay.Observations.for_ObservationSnapshot.when_creating;

public class with_negative_counts : Specification
{
    [Theory]
    [InlineData("appends")]
    [InlineData("edges")]
    [InlineData("unattributed")]
    void should_refuse_negative_event_counts(string negative)
    {
        var type = new ObservedEventType("Recorded", 1);
        var builder = new snapshot_builder
        {
            Appends = [new(type, negative == "appends" ? -1 : 1)],
            Edges = [new(new(ObservedProducerKind.Command, "Record"), type, negative == "edges" ? -1 : 0)],
            Unattributed = [new(type, negative == "unattributed" ? -1 : 0)]
        };
        Catch.Exception(() => builder.Build()).ShouldBeOfExactType<InvalidObservationSnapshot>();
    }
}
