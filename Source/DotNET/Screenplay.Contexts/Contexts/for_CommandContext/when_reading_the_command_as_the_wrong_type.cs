// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts.for_CommandContext;

public class when_reading_the_command_as_the_wrong_type : Specification
{
    CommandContext _context;
    Exception _error;

    void Establish() => _context = new("invoice", TenantId.Default, Identity.NotSet, CausedBy.NotSet, Causation.NotSet, DateTimeOffset.UtcNow);
    void Because() => _error = Catch.Exception(() => _context.CommandAs<int>());

    [Fact] void should_throw_a_payload_type_mismatch() => _error.ShouldBeOfExactType<ContextPayloadTypeMismatch>();
    [Fact] void should_name_the_member_and_types() => _error.Message.ShouldEqual("Context payload 'Command' is System.String, not the requested type System.Int32.");
}
