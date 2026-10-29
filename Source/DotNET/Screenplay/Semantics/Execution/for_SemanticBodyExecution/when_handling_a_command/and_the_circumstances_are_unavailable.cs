// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_handling_a_command;

public class and_the_circumstances_are_unavailable : given.a_command_handler
{
    void Establish() => Request = Request with { Caller = null, Tenant = null, Occurrence = null };
    void Because() => Result = Execute();

    [Fact] void should_accept() => Result.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_not_invent_identity() => Contexts.Single().Identity.ShouldBeNull();
    [Fact] void should_not_invent_audit_identity() => Contexts.Single().CausedBy.ShouldBeNull();
    [Fact] void should_not_invent_a_tenant() => Contexts.Single().Tenant.ShouldBeNull();
    [Fact] void should_not_invent_time() => Contexts.Single().Occurred.ShouldBeNull();
}
