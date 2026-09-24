// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts.for_ReducerContext;

public class when_reading_the_first_state_as_a_nullable_value : Specification
{
    ReducerContext _context;
    int? _state;

    void Establish() => _context = new(null, 42, "invoice", TenantId.Default, DateTimeOffset.UtcNow, 1);
    void Because() => _state = _context.StateAs<int?>();

    [Fact] void should_return_null() => _state.ShouldBeNull();
}
