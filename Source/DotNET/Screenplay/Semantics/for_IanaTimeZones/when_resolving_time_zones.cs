// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_IanaTimeZones;

public class when_resolving_time_zones : Specification
{
    [Theory]
    [InlineData("Europe/Oslo", true)]
    [InlineData("UTC", true)]
    [InlineData("Mars/Olympus", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void should_report_whether_the_id_is_a_known_iana_zone(string? id, bool expected) => IanaTimeZones.IsKnown(id).ShouldEqual(expected);

    [Fact]
    void should_never_replace_an_unknown_zone_by_utc()
    {
        IanaTimeZones.TryFind("Mars/Olympus", out _).ShouldBeFalse();
    }
}
