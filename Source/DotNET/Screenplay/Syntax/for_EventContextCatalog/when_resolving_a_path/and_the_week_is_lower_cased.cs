// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.for_EventContextCatalog.when_resolving_a_path;

public class and_the_week_is_lower_cased : Specification
{
    EventContextPathResolution _resolution;

    void Because() => _resolution = EventContextCatalog.Resolve("occurred.week");

    [Fact] void should_not_know_it() => _resolution.Status.ShouldEqual(EventContextPathStatus.UnknownSubPath);
    [Fact] void should_name_the_segment() => _resolution.Segment.ShouldEqual("week");
    [Fact] void should_expect_the_week() => _resolution.Expected.Select(member => member.Name).ShouldContainOnly("Week");
}
