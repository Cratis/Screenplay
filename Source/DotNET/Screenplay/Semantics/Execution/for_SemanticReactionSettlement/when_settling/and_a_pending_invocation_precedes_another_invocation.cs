// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_settling;

public class and_a_pending_invocation_precedes_another_invocation : given.a_settlement_with_an_ability
{
    void Establish()
    {
        PrepareAbility(additionalInvocation: """
                      invokes Carrier.Send
                        message = "next"
                        on returned
                          produces Completed
            """);
        _invoker.InvokeAsync(Arg.Any<SemanticAbilityInvocationContext>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _calls.Add(call.Arg<SemanticAbilityInvocationContext>());
            return new ValueTask<SemanticAbilityAnswer>(Answer(_calls.Count == 1 ? SemanticAbilityAnswerKind.Pending : SemanticAbilityAnswerKind.Returned));
        });
    }

    async Task Because() => await SettleAsync();

    [Fact] void should_continue_with_the_later_invocation() => _calls.Count.ShouldEqual(2);
    [Fact] void should_produce_only_the_later_branch() => _accepted.Facts.Single().EventContract.ShouldEqual(EventId("Completed"));
    [Fact] void should_keep_only_the_first_invocation_pending() => _accepted.PendingInvocations.Single().InvocationIndex.ShouldEqual(0);
}
