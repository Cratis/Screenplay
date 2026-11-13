// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_settling;

public class and_an_invoker_returns_values : given.a_settlement_with_an_ability
{
    async Task Because() => await SettleAsync();

    [Fact] void should_map_the_returned_value() => Value(_accepted.Facts.Single()).ShouldEqual(SemanticValue.Text("returned"));
    [Fact] void should_invoke_once() => _calls.Count.ShouldEqual(1);
    [Fact] void should_pass_the_system() => _calls.Single().System.ShouldEqual(_answer.System);
    [Fact] void should_pass_the_ability() => _calls.Single().Ability.ShouldEqual("Send");
    [Fact] void should_pass_evaluated_inputs() => _calls.Single().Inputs.Single().ShouldEqual(new SemanticAbilityFieldValue("message", SemanticValue.Text("mapped")));
    [Fact] void should_pass_the_cause() => _calls.Single().Cause.ShouldEqual(_input);
    [Fact] void should_pass_the_occurrence() => _calls.Single().Occurred.ShouldEqual(_occurred);
    [Fact] void should_identify_the_reaction() => _calls.Single().Reaction.ShouldEqual(_plan.RunningReactions.Single().Id);
    [Fact] void should_identify_the_cause_sequence() => _calls.Single().Identity.CauseSequence.ShouldEqual(0L);
    [Fact] void should_identify_the_invocation_index() => _calls.Single().Identity.Index.ShouldEqual(0);
    [Fact] void should_have_no_pending_invocations() => _accepted.PendingInvocations.ShouldBeEmpty();
}
