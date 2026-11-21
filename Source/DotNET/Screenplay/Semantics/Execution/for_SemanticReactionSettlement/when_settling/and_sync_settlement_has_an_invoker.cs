// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_settling;

public class and_sync_settlement_has_an_invoker : given.a_settlement_with_an_ability
{
    Exception _caught;

    void Because()
    {
        try { _settlement.Settle(_plan, _world, [_input], _occurred); }
        catch (InvalidOperationException exception) { _caught = exception; }
    }

    [Fact] void should_refuse_to_run_host_work_synchronously() => _caught.ShouldNotBeNull();
    [Fact] void should_not_invoke_the_host() => _calls.ShouldBeEmpty();
    [Fact] void should_leave_the_world_unchanged() => _world.Facts.Length.ShouldEqual(1);
}
