// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.given;

public class a_settlement_with_an_ability : a_settlement
{
    protected ISemanticAbilityInvoker _invoker;
    protected readonly List<SemanticAbilityInvocationContext> _calls = [];
    protected SemanticAbilityAnswer _answer;
    protected SemanticAccepted _accepted;

    void Establish()
    {
        PrepareAbility();
        _invoker = Substitute.For<ISemanticAbilityInvoker>();
        _invoker.InvokeAsync(Arg.Any<SemanticAbilityInvocationContext>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _calls.Add(call.Arg<SemanticAbilityInvocationContext>());
            return new ValueTask<SemanticAbilityAnswer>(_answer);
        });
        _settlement = new(_evaluator, _invoker);
        _answer = Answer(SemanticAbilityAnswerKind.Returned) with { Values = [new("value", SemanticValue.Text("returned"))] };
    }

    protected void PrepareAbility(string extra = "", string returned = "value = $result.value", string additionalInvocation = "")
    {
        Compile(
            $$"""
            system Carrier
              ability Send idempotent
                message String
                returns
                  value String
                outcome Busy
            module M
              feature F
                slice Automation Work
                  event Started
                  event Continued
                    value String
                  event Unavailable
                    value String
                  event Failed
                    value String
                  event Completed
                  reaction Send
                    when Started
                      invokes Carrier.Send
                        message = "mapped"
                        on returned
                          produces Continued
                            {{returned}}
                        on Busy
                          produces Unavailable
                            value = $outcome
                        on failed
                          produces Failed
                            value = $reason
            {{additionalInvocation}}
            {{extra}}
            """);
        _input = new(EventId("Started"), SemanticValue.Text("one"), [])
        {
            Context = new(new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), SemanticValue.Text("one")))
        };
        _world = SemanticWorld.Create([_input], []);
    }

    protected SemanticAbilityAnswer Answer(SemanticAbilityAnswerKind kind) => new(_plan.Model.Application.Systems.Single().Id, "Send", kind);

    protected async Task SettleAsync()
    {
        _result = await _settlement.SettleAsync(_plan, _world, [_input], _occurred);
        _accepted = _result as SemanticAccepted;
    }

    protected static SemanticValue Value(SemanticFact fact) => fact.Values.Single().Value;
}
