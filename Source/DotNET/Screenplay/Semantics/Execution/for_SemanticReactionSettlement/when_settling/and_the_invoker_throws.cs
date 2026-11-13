// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_settling;

public class and_the_invoker_throws : given.a_settlement_with_an_ability
{
    readonly InvalidSemanticContract _failure = new("host failure");
    Exception _caught;

    void Establish() => _invoker.InvokeAsync(Arg.Any<SemanticAbilityInvocationContext>(), Arg.Any<CancellationToken>()).Returns(_ => new ValueTask<SemanticAbilityAnswer>(Task.FromException<SemanticAbilityAnswer>(_failure)));

    async Task Because()
    {
        try { await SettleAsync(); }
        catch (Exception exception) { _caught = exception; }
    }

    [Fact] void should_propagate_even_a_contract_exception_from_the_host() => _caught.ShouldEqual(_failure);
    [Fact] void should_leave_the_world_unchanged() => _world.Facts.Length.ShouldEqual(1);
}
