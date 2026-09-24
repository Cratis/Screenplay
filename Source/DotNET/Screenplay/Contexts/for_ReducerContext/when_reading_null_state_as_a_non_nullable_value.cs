// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts.for_ReducerContext;

public class when_reading_null_state_as_a_non_nullable_value : Specification
{
    ReducerContext _context;
    Exception _error;

    void Establish() => _context = new(null, 42, "invoice", TenantId.Default, DateTimeOffset.UtcNow, 1);
    void Because() => _error = Catch.Exception(() => _context.StateAs<int>());

    [Fact] void should_name_the_null_state_and_requested_type() => _error.Message.ShouldEqual("Context payload 'State' is null, not the requested type System.Int32.");
}
