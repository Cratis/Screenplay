// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_settling;

public class and_no_invoker_is_configured : given.a_settlement_with_an_ability
{
    void Establish()
    {
        PrepareAbility(returned: "value = \"default\"");
        _settlement = new(_evaluator);
    }

    void Because() => _accepted = (SemanticAccepted)_settlement.Settle(_plan, _world, [_input], _occurred);

    [Fact] void should_produce_the_default_returned_branch() => _accepted.Facts.Single().EventContract.ShouldEqual(EventId("Continued"));
    [Fact] void should_keep_the_default_value() => Value(_accepted.Facts.Single()).ShouldEqual(SemanticValue.Text("default"));
    [Fact] void should_have_no_pending_invocations() => _accepted.PendingInvocations.ShouldBeEmpty();
    [Fact] void should_not_invoke_the_host() => _calls.ShouldBeEmpty();
}
