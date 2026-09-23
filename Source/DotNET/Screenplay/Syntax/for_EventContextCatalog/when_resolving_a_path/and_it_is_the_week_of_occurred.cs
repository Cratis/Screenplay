// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.for_EventContextCatalog.when_resolving_a_path;

public class and_it_is_the_week_of_occurred : Specification
{
    EventContextPathResolution _written;
    EventContextPathResolution _called;

    void Because()
    {
        _written = EventContextCatalog.Resolve("occurred.Week");
        _called = EventContextCatalog.Resolve("occurred.Week()");
    }

    [Fact] void should_know_it_without_parentheses() => _written.IsKnown.ShouldBeTrue();
    [Fact] void should_know_it_with_parentheses() => _called.IsKnown.ShouldBeTrue();
    [Fact] void should_resolve_to_the_function() => _written.Member!.Kind.ShouldEqual(EventContextMemberKind.Function);
}
