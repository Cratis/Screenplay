// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_answering;

public class and_the_cause_sequence_is_outside_the_world : given.a_pending_invocation
{
    [Theory]
    [InlineData(-1L)]
    [InlineData(1L)]
    [InlineData(long.MaxValue)]
    async Task should_reject_without_changing_the_world(long sequence)
    {
        var pending = _pending with { Identity = _pending.Identity with { CauseSequence = sequence } };
        var result = await _settlement.SettleAnswerAsync(_plan, _world, pending, Answer(SemanticAbilityAnswerKind.Returned));
        result.ShouldBeOfExactType<SemanticRejected>();
        var rejected = (SemanticRejected)result;
        rejected.Category.ShouldEqual(SemanticRejectionCategory.Contract);
        rejected.World.ShouldEqual(_world);
        _calls.Count.ShouldEqual(1);
    }
}
