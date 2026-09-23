// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.for_EventContextCatalog.when_resolving_a_path;

public class and_it_is_every_listed_path : Specification
{
    List<EventContextPathResolution> _resolutions;

    void Because() => _resolutions = [.. EventContextCatalog.Paths.Select(path => EventContextCatalog.Resolve(path.Path))];

    [Fact] void should_list_paths_below_the_members() => EventContextCatalog.Paths.Count.ShouldBeGreaterThan(EventContextCatalog.Members.Count);
    [Fact] void should_know_every_one_of_them() => _resolutions.Where(resolution => !resolution.IsKnown).Select(resolution => resolution.Path).ShouldBeEmpty();
    [Fact] void should_resolve_each_to_the_member_it_lists() => _resolutions.Select(resolution => resolution.Member).ShouldContainOnly([.. EventContextCatalog.Paths.Select(path => path.Member)]);
}
