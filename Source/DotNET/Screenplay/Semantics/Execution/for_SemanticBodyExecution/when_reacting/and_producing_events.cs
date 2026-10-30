// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_reacting;

public class and_producing_events : given.a_reaction_effect
{
    void Because() => Fire();

    [Fact] void should_accept_the_effect() => Failure.ShouldBeNull();
    [Fact] void should_call_the_provider_once() => Provider.Calls.ShouldEqual(1);
    [Fact] void should_keep_the_destination() => Loop.Facts.Single().Destination.ShouldEqual(Cause.Destination);
    [Fact] void should_keep_the_context() => Loop.Facts.Single().Context.ShouldEqual(Cause.Context);
    [Fact] void should_keep_the_occurrence() => Loop.Facts.Single().Occurred.ShouldEqual(Occurred);
    [Fact] void should_record_the_reaction_origin() => Loop.Facts.Single().ReactionOrigin.ShouldEqual(Reaction.Id);
    [Fact] void should_pass_the_typed_event() => Context.Event.Properties.ShouldEqual(Cause.Values);
    [Fact] void should_pass_the_event_source() => Context.EventSourceId.ShouldEqual(Cause.Destination);
    [Fact] void should_pass_the_occurrence() => Context.Occurred.ShouldEqual(Occurred);
    [Fact] void should_not_fabricate_tenant() => Context.Tenant.ShouldBeNull();
    [Fact] void should_not_fabricate_audit_identity() => Context.CausedBy.ShouldBeNull();
    [Fact] void should_not_fabricate_causation() => Context.Causation.ShouldBeNull();
    [Fact] void should_keep_system_identity_separate() => Context.RunsAs.ShouldEqual(Reaction.RunsAs);
    [Fact] void should_leave_the_provider_live() => Provider.Disposals.ShouldEqual(0);
}
