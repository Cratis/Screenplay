// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.for_EventContextCatalog.when_resolving_a_path;

public class and_it_follows_on_behalf_of_below_its_listed_depth : Specification
{
    EventContextPathResolution _resolution;

    void Because() => _resolution = EventContextCatalog.Resolve("causedBy.onBehalfOf.onBehalfOf.userName");

    [Fact] void should_not_list_the_path() => EventContextCatalog.Paths.Select(path => path.Path).ShouldNotContain("causedBy.onBehalfOf.onBehalfOf.userName");
    [Fact] void should_still_know_it() => _resolution.IsKnown.ShouldBeTrue();
}
