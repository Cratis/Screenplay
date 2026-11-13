// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_settling;

public class and_a_pending_invocation_is_not_in_the_plan : given.a_settlement_with_an_ability
{
    SemanticExecutionResult _resumed;

    void Establish() => _answer = Answer(SemanticAbilityAnswerKind.Pending);

    async Task Because()
    {
        await SettleAsync();
        var pending = _accepted.PendingInvocations.Single();
        pending = pending with { Identity = pending.Identity with { Reaction = EventId("Started") } };
        _resumed = await _settlement.SettleAnswerAsync(_plan, _accepted.World, pending, Answer(SemanticAbilityAnswerKind.Returned));
    }

    [Fact] void should_reject_a_changed_plan() => _resumed.ShouldBeOfExactType<SemanticRejected>();
    [Fact] void should_keep_the_prior_world() => _resumed.World.ShouldEqual(_accepted.World);
    [Fact] void should_not_invoke_the_host_again() => _calls.Count.ShouldEqual(1);
}
