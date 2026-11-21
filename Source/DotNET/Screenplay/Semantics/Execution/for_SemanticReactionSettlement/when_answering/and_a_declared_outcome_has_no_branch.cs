// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_answering;

public class and_a_declared_outcome_has_no_branch : given.a_pending_invocation
{
    SemanticAccepted _resumed;

    async Task Because() => _resumed = (SemanticAccepted)await _settlement.SettleAnswerAsync(_plan, _world, _pending,
        Answer(SemanticAbilityAnswerKind.Outcome) with { Outcome = "Unhandled" });

    [Fact] void should_produce_no_facts() => _resumed.Facts.ShouldBeEmpty();
    [Fact] void should_keep_the_world() => _resumed.World.ShouldEqual(_world);
    [Fact] void should_not_reinvoke_the_ability() => _calls.Count.ShouldEqual(1);
}
