// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_answering;

public class and_the_cause_no_longer_matches_the_where : given.a_settlement_with_an_ability
{
    SemanticPendingAbilityInvocation _pending;
    SemanticAccepted _resumed;

    void Establish()
    {
        Compile(
            """
            system Carrier
              ability Send idempotent
                message String
                returns
                  value String
            module M
              feature F
                slice Automation Work
                  event Started
                    allowed Bool
                  event Continued
                    value String
                  reaction Send
                    when Started
                      allowed
                      invokes Carrier.Send
                        message = "excluded"
                        on returned
                          produces Continued
                            value = "wrong branch"
                    where allowed == false
                    when Started
                      allowed
                      invokes Carrier.Send
                        message = "included"
                        on returned
                          produces Continued
                            value = $result.value
                    where allowed == true
            """);
        var allowed = _plan.Events[EventId("Started")].Properties.Single().Id;
        _input = _input with { EventContract = EventId("Started"), Values = [new(allowed, SemanticValue.Boolean(true))] };
        _world = SemanticWorld.Create([_input], []);
        _answer = Answer(SemanticAbilityAnswerKind.Pending);
    }

    async Task Because()
    {
        await SettleAsync();
        _pending = _accepted.PendingInvocations.Single();
        var changedCause = _pending.Cause with { Values = [new(_pending.Cause.Values.Single().TargetProperty, SemanticValue.Boolean(false))] };
        var completed = Answer(SemanticAbilityAnswerKind.Returned) with { Values = [new("value", SemanticValue.Text("resumed"))] };
        _resumed = (SemanticAccepted)await _settlement.SettleAnswerAsync(_plan, _world, _pending with { Cause = changedCause }, completed);
    }

    [Fact] void should_count_the_excluded_trigger_in_the_locator() => _pending.TriggerIndex.ShouldEqual(1);
    [Fact] void should_use_the_same_trigger_ordinal_in_the_identity() => _pending.Identity.TriggerIndex.ShouldEqual(_pending.TriggerIndex);
    [Fact] void should_keep_the_raw_invocation_ordinal() => _pending.InvocationIndex.ShouldEqual(0);
    [Fact] void should_resume_the_original_branch_without_rechecking_where() => Value(_resumed.Facts.Single()).ShouldEqual(SemanticValue.Text("resumed"));
    [Fact] void should_not_invoke_the_now_matching_trigger() => _calls.Count.ShouldEqual(1);
}
