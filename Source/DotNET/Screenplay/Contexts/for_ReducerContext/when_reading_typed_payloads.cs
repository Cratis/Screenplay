// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts.for_ReducerContext;

public class when_reading_typed_payloads : Specification
{
    ReducerContext _context;
    int _total;
    int _event;

    void Establish() => _context = new(new List<int> { 20, 22 }, 42, "invoice", TenantId.Default, DateTimeOffset.UtcNow, 1);
    void Because()
    {
        _total = _context.StateAs<List<int>>()!.Sum();
        _event = _context.EventAs<int>();
    }

    [Fact] void should_bind_linq_to_the_state() => _total.ShouldEqual(42);
    [Fact] void should_read_the_typed_event() => _event.ShouldEqual(42);
    [Fact] void should_preserve_the_dynamic_members() => ((int)_context.Event).ShouldEqual(((List<int>)_context.State).Sum());
}
