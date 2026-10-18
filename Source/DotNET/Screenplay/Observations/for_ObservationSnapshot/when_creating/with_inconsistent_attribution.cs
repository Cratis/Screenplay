// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Observations.for_ObservationComparison.given;

namespace Cratis.Screenplay.Observations.for_ObservationSnapshot.when_creating;

public class with_inconsistent_attribution : Specification
{
    [Fact]
    void should_refuse_attribution_exceeding_appends()
    {
        var builder = new snapshot_builder
        {
            Appends = [new(new("Registered", 1), 1)],
            Edges = [new(new(ObservedProducerKind.Command, "Register"), new("Registered", 1), 1)],
            Unattributed = [new(new("Registered", 1), 1)]
        };
        Catch.Exception(() => builder.Build()).ShouldBeOfExactType<InvalidObservationSnapshot>();
    }
}
