// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_answering;

public class and_the_world_has_moved_on : given.a_pending_invocation
{
    SemanticAccepted _immediate;
    SemanticAccepted _resumed;

    async Task Because()
    {
        _answer = Answer(SemanticAbilityAnswerKind.Returned) with { Values = [new("value", SemanticValue.Text("later"))] };
        _immediate = (SemanticAccepted)await _settlement.SettleAsync(_plan, _world, [_input], _occurred);
        var moved = SemanticWorld.Create([_input, new(EventId("Completed"), SemanticValue.Text("other"), [])], []);
        _resumed = (SemanticAccepted)await _settlement.SettleAnswerAsync(_plan, moved, _pending, _answer);
    }

    [Fact] void should_produce_the_same_fact_as_an_immediate_answer() => _resumed.Facts.AsEnumerable().ShouldEqual(_immediate.Facts);
    [Fact] void should_keep_the_intervening_fact() => _resumed.World.Facts.Length.ShouldEqual(3);
    [Fact] void should_not_reinvoke_on_resume() => _calls.Count.ShouldEqual(2);
}
