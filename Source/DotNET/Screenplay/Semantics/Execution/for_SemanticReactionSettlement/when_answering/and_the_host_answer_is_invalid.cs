// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_answering;

public class and_the_host_answer_is_invalid : given.a_pending_invocation
{
    [Theory]
    [MemberData(nameof(given.invalid_ability_answers.Cases), MemberType = typeof(given.invalid_ability_answers))]
    async Task should_reject_without_changing_the_world(string defect)
    {
        var valid = Answer(SemanticAbilityAnswerKind.Returned) with { Values = [new("value", SemanticValue.Text("returned"))] };
        var answer = given.invalid_ability_answers.Create(valid, defect);
        var result = await _settlement.SettleAnswerAsync(_plan, _world, _pending, answer);
        result.ShouldBeOfExactType<SemanticRejected>();
        var rejected = (SemanticRejected)result;
        rejected.Category.ShouldEqual(SemanticRejectionCategory.Contract);
        rejected.World.ShouldEqual(_world);
        _calls.Count.ShouldEqual(1);
    }
}
