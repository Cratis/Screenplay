// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.for_EventContextCatalog.when_resolving_a_path;

public class and_the_sub_path_is_unknown : Specification
{
    EventContextPathResolution _resolution;

    void Because() => _resolution = EventContextCatalog.Resolve("eventType.name");

    [Fact] void should_report_an_unknown_sub_path() => _resolution.Status.ShouldEqual(EventContextPathStatus.UnknownSubPath);
    [Fact] void should_name_the_segment() => _resolution.Segment.ShouldEqual("name");
    [Fact] void should_name_the_member_it_sits_below() => _resolution.Member!.Name.ShouldEqual("eventType");
    [Fact] void should_expect_the_members_of_the_event_type() => _resolution.Expected.Select(member => member.Name).ShouldContainOnly("id", "generation", "tombstone");
}
