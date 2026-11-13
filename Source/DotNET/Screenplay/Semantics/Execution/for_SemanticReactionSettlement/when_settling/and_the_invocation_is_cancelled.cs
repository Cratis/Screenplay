// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_settling;

public class and_the_invocation_is_cancelled : given.a_settlement_with_an_ability
{
    CancellationTokenSource _cancellation;
    CancellationToken _received;
    Exception _caught;

    void Establish()
    {
        _cancellation = new();
        _invoker.InvokeAsync(Arg.Any<SemanticAbilityInvocationContext>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            _received = call.Arg<CancellationToken>();
            _cancellation.Cancel();
            await Task.Delay(Timeout.Infinite, _received);
            return _answer;
        });
    }

    async Task Because()
    {
        try { await _settlement.SettleAsync(_plan, _world, [_input], _occurred, _cancellation.Token); }
        catch (OperationCanceledException exception) { _caught = exception; }
    }

    void Destroy() => _cancellation.Dispose();

    [Fact] void should_pass_the_token_to_the_host() => _received.ShouldEqual(_cancellation.Token);
    [Fact] void should_propagate_cancellation() => _caught.ShouldNotBeNull();
    [Fact] void should_leave_the_world_unchanged() => _world.Facts.Length.ShouldEqual(1);
}
