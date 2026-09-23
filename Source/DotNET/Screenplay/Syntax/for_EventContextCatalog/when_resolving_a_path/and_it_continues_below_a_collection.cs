// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.for_EventContextCatalog.when_resolving_a_path;

public class and_it_continues_below_a_collection : Specification
{
    EventContextPathResolution _causation;
    EventContextPathResolution _tags;

    void Because()
    {
        _causation = EventContextCatalog.Resolve("causation.occurred");
        _tags = EventContextCatalog.Resolve("tags.value");
    }

    [Fact] void should_reject_a_path_below_causation() => _causation.Status.ShouldEqual(EventContextPathStatus.BelowCollection);
    [Fact] void should_reject_a_path_below_tags() => _tags.Status.ShouldEqual(EventContextPathStatus.BelowCollection);
    [Fact] void should_name_the_collection() => _causation.Member!.Name.ShouldEqual("causation");
    [Fact] void should_still_know_the_collection_itself() => EventContextCatalog.Resolve("causation").IsKnown.ShouldBeTrue();
}
