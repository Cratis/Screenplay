// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.for_EventContextCatalog.when_resolving_a_path;

public class and_the_member_is_unknown : Specification
{
    EventContextPathResolution _resolution;

    void Because() => _resolution = EventContextCatalog.Resolve("causationId");

    [Fact] void should_report_an_unknown_member() => _resolution.Status.ShouldEqual(EventContextPathStatus.UnknownMember);
    [Fact] void should_name_the_segment() => _resolution.Segment.ShouldEqual("causationId");
    [Fact] void should_have_resolved_no_member() => _resolution.Member.ShouldBeNull();
    [Fact] void should_expect_the_members_of_the_event_context() => _resolution.Expected.ShouldEqual(EventContextCatalog.Members);
}
