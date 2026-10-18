// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Observations.for_ObservationComparison.given;

namespace Cratis.Screenplay.Observations.for_ObservationSnapshot.when_creating;

public class with_an_inconsistent_range : Specification
{
    [Theory]
    [InlineData(3L, 2L, 4L)]
    [InlineData(0L, 5L, 4L)]
    [InlineData(-1L, 2L, 4L)]
    [InlineData(null, 2L, 4L)]
    [InlineData(0L, null, 4L)]
    [InlineData(0L, 2L, null)]
    void should_refuse_inconsistent_bounds(long? first, long? last, long? tail)
    {
        var builder = new snapshot_builder { Coverage = new("log", true, first, last, tail, []) };
        Catch.Exception(() => builder.Build()).ShouldBeOfExactType<InvalidObservationSnapshot>();
    }

    [Fact]
    void should_refuse_overlapping_gaps()
    {
        var builder = new snapshot_builder { Coverage = new("log", true, 0, 10, 10, [new(2, 5), new(4, 6)]) };
        Catch.Exception(() => builder.Build()).ShouldBeOfExactType<InvalidObservationSnapshot>();
    }
}
