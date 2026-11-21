// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_settling;

public class and_the_host_defers_an_invocation : given.a_settlement_with_an_ability
{
    void Establish() => _answer = Answer(SemanticAbilityAnswerKind.Pending);
    async Task Because() => await SettleAsync();

    [Fact] void should_accept_the_batch() => _result.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_produce_no_branch_facts() => _accepted.Facts.ShouldBeEmpty();
    [Fact] void should_keep_the_world() => _accepted.World.ShouldEqual(_world);
    [Fact] void should_return_the_pending_identity() => _accepted.PendingInvocations.Single().Identity.ShouldEqual(_calls.Single().Identity);
    [Fact] void should_return_the_pending_system() => _accepted.PendingInvocations.Single().System.ShouldEqual(_calls.Single().System);
    [Fact] void should_return_the_pending_ability() => _accepted.PendingInvocations.Single().Ability.ShouldEqual("Send");
    [Fact] void should_return_the_pending_inputs() => _accepted.PendingInvocations.Single().Inputs.AsEnumerable().ShouldEqual(_calls.Single().Inputs);
    [Fact] void should_retain_the_cause() => _accepted.PendingInvocations.Single().Cause.ShouldEqual(_input);
    [Fact] void should_retain_the_occurrence() => _accepted.PendingInvocations.Single().Occurred.ShouldEqual(_occurred);
}
