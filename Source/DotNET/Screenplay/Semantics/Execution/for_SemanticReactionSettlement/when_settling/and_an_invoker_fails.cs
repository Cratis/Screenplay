// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_settling;

public class and_an_invoker_fails : given.a_settlement_with_an_ability
{
    void Establish() => _answer = Answer(SemanticAbilityAnswerKind.Failed) with { Reason = "unavailable" };
    async Task Because() => await SettleAsync();

    [Fact] void should_choose_the_failure_branch() => _accepted.Facts.Single().EventContract.ShouldEqual(EventId("Failed"));
    [Fact] void should_map_the_reason() => Value(_accepted.Facts.Single()).ShouldEqual(SemanticValue.Text("unavailable"));
}
