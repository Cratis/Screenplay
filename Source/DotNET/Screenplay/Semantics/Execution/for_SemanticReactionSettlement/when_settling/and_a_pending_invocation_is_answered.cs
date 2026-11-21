// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_settling;

public class and_a_pending_invocation_is_answered : given.a_settlement_with_an_ability
{
    SemanticAccepted _resumed;

    void Establish()
    {
        PrepareAbility(extra: """
                  reaction Finish
                    when Continued
                      produces Completed
            """);
        _answer = Answer(SemanticAbilityAnswerKind.Pending);
    }

    async Task Because()
    {
        await SettleAsync();
        var completed = Answer(SemanticAbilityAnswerKind.Returned) with { Values = [new("value", SemanticValue.Text("later"))] };
        _resumed = (SemanticAccepted)await new SemanticReactionSettlement(_evaluator, _invoker).SettleAnswerAsync(_plan, _accepted.World, _accepted.PendingInvocations.Single(), completed);
    }

    [Fact] void should_apply_only_the_answer_branch_and_its_cascade() => _resumed.Facts.Select(fact => fact.EventContract).ShouldEqual([EventId("Continued"), EventId("Completed")]);
    [Fact] void should_use_the_later_value() => Value(_resumed.Facts[0]).ShouldEqual(SemanticValue.Text("later"));
    [Fact] void should_not_invoke_the_pending_ability_again() => _calls.Count.ShouldEqual(1);
    [Fact] void should_restore_the_cause_context() => _resumed.Facts[0].Context.ShouldEqual(_input.Context);
    [Fact] void should_restore_the_occurrence() => _resumed.Facts[0].Occurred.ShouldEqual(_occurred);
    [Fact] void should_clear_pending_invocations() => _resumed.PendingInvocations.ShouldBeEmpty();
    [Fact] void should_leave_the_prior_world_unchanged() => _accepted.World.Facts.Length.ShouldEqual(1);
}
