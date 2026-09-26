// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts.for_ReducerContext;

public class when_reading_the_state_as_the_wrong_type : Specification
{
    ReducerContext _context;
    Exception _error;

    void Establish() => _context = new("invoice", 42, "invoice", TenantId.Default, DateTimeOffset.UtcNow, 1);
    void Because() => _error = Catch.Exception(() => _context.StateAs<int>());

    [Fact] void should_name_the_state_and_types() => _error.Message.ShouldEqual("Context payload 'State' is System.String, not the requested type System.Int32.");
}
