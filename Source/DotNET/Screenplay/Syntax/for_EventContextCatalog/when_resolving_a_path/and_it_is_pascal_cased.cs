// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.for_EventContextCatalog.when_resolving_a_path;

public class and_it_is_pascal_cased : Specification
{
    EventContextPathResolution _resolution;

    void Because() => _resolution = EventContextCatalog.Resolve("EventType.Id");

    [Fact] void should_know_it() => _resolution.IsKnown.ShouldBeTrue();
    [Fact] void should_resolve_to_the_event_type_id() => _resolution.Member!.Type.ShouldEqual("EventTypeId");
}
