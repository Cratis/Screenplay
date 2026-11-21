// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_answering;

public class and_the_reaction_is_unknown : given.a_pending_invocation
{
    async Task Because() => _result = await _settlement.SettleAnswerAsync(_plan, _world,
        _pending with { Identity = _pending.Identity with { Reaction = default } }, Answer(SemanticAbilityAnswerKind.Returned));

    [Fact] void should_reject_the_answer() => _result.ShouldBeOfExactType<SemanticRejected>();
    [Fact] void should_reject_the_contract() => ((SemanticRejected)_result).Category.ShouldEqual(SemanticRejectionCategory.Contract);
    [Fact] void should_keep_the_world() => _result.World.ShouldEqual(_world);
    [Fact] void should_not_reinvoke_the_ability() => _calls.Count.ShouldEqual(1);
}
