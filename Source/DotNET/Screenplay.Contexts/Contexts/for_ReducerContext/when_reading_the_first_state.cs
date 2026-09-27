// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts.for_ReducerContext;

public class when_reading_the_first_state : Specification
{
    ReducerContext _context;
    List<int>? _state;

    void Establish() => _context = new(null, 42, "invoice", TenantId.Default, DateTimeOffset.UtcNow, 1);
    void Because() => _state = _context.StateAs<List<int>>();

    [Fact] void should_return_null() => _state.ShouldBeNull();
    [Fact] void should_preserve_the_first_event_flag() => _context.IsFirst.ShouldBeTrue();
}
