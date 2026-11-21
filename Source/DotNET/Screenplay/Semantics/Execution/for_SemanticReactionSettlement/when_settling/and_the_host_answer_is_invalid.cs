// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_settling;

public class and_the_host_answer_is_invalid : given.a_settlement_with_an_ability
{
    [Theory]
    [MemberData(nameof(given.invalid_ability_answers.Cases), MemberType = typeof(given.invalid_ability_answers))]
    async Task should_reject_without_changing_the_world(string defect)
    {
        _answer = given.invalid_ability_answers.Create(_answer, defect);
        var result = await _settlement.SettleAsync(_plan, _world, [_input], _occurred);
        result.ShouldBeOfExactType<SemanticRejected>();
        var rejected = (SemanticRejected)result;
        rejected.Category.ShouldEqual(SemanticRejectionCategory.Contract);
        rejected.World.ShouldEqual(_world);
        _calls.Count.ShouldEqual(1);
    }
}
