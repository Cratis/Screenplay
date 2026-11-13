// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_settling;

public class and_an_outcome_has_no_branch : given.a_settlement_with_an_ability
{
    void Establish() => _answer = Answer(SemanticAbilityAnswerKind.Outcome) with { Outcome = "Unknown" };
    async Task Because() => await SettleAsync();

    [Fact] void should_accept_without_producing_a_fact() => _accepted.Facts.ShouldBeEmpty();
    [Fact] void should_keep_the_world() => _accepted.World.ShouldEqual(_world);
}
