// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_settling;

public class and_an_invoker_answers_an_outcome : given.a_settlement_with_an_ability
{
    void Establish() => _answer = Answer(SemanticAbilityAnswerKind.Outcome) with { Outcome = "Busy" };
    async Task Because() => await SettleAsync();

    [Fact] void should_choose_the_outcome_branch() => _accepted.Facts.Single().EventContract.ShouldEqual(EventId("Unavailable"));
    [Fact] void should_map_the_outcome() => Value(_accepted.Facts.Single()).ShouldEqual(SemanticValue.Text("Busy"));
}
