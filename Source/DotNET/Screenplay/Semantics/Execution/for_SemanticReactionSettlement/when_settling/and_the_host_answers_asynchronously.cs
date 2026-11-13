// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_settling;

public class and_the_host_answers_asynchronously : given.a_settlement_with_an_ability
{
    readonly TaskCompletionSource<SemanticAbilityAnswer> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _waited;

    void Establish() => _invoker.InvokeAsync(Arg.Any<SemanticAbilityInvocationContext>(), Arg.Any<CancellationToken>()).Returns(_ => new ValueTask<SemanticAbilityAnswer>(_completion.Task));

    async Task Because()
    {
        var settling = _settlement.SettleAsync(_plan, _world, [_input], _occurred);
        _waited = !settling.IsCompleted;
        _completion.SetResult(_answer);
        _accepted = (SemanticAccepted)await settling;
    }

    [Fact] void should_yield_while_the_host_is_working() => _waited.ShouldBeTrue();
    [Fact] void should_apply_the_answer_after_awaiting_it() => Value(_accepted.Facts.Single()).ShouldEqual(SemanticValue.Text("returned"));
}
