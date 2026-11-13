// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_settling;

public class and_a_later_cascade_fails : given.a_settlement_with_an_ability
{
    void Establish()
    {
        PrepareAbility(extra: """
                  event CompletedWithValue
                    value String
                  reaction Finish
                    when Continued
                      invokes Carrier.Send
                        message = "second"
                        on returned
                          produces CompletedWithValue
                            value = $result.value
            """);
        var calls = 0;
        _invoker.InvokeAsync(Arg.Any<SemanticAbilityInvocationContext>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _calls.Add(call.Arg<SemanticAbilityInvocationContext>());
            return new ValueTask<SemanticAbilityAnswer>(++calls == 1 ? _answer : Answer(SemanticAbilityAnswerKind.Returned) with { Values = [new("value", SemanticValue.Number(42))] });
        });
    }

    async Task Because() => await SettleAsync();

    [Fact] void should_reject_the_cascade() => _result.ShouldBeOfExactType<SemanticRejected>();
    [Fact] void should_retain_the_input_world() => _result.World.ShouldEqual(_world);
    [Fact] void should_leave_the_input_facts_unchanged() => _world.Facts.Length.ShouldEqual(1);
    [Fact] void should_have_observed_the_host_calls() => _calls.Count.ShouldEqual(2);
}
