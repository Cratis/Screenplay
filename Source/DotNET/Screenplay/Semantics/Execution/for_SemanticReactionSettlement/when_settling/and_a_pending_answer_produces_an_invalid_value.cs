// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_settling;

public class and_a_pending_answer_produces_an_invalid_value : given.a_settlement_with_an_ability
{
    SemanticExecutionResult _resumed;

    void Establish() => _answer = Answer(SemanticAbilityAnswerKind.Pending);

    async Task Because()
    {
        await SettleAsync();
        var completed = Answer(SemanticAbilityAnswerKind.Returned) with { Values = [new("value", SemanticValue.Number(42))] };
        _resumed = await _settlement.SettleAnswerAsync(_plan, _accepted.World, _accepted.PendingInvocations.Single(), completed);
    }

    [Fact] void should_reject_the_branch() => _resumed.ShouldBeOfExactType<SemanticRejected>();
    [Fact] void should_keep_the_prior_world() => _resumed.World.ShouldEqual(_accepted.World);
    [Fact] void should_not_invoke_the_host_again() => _calls.Count.ShouldEqual(1);
}
