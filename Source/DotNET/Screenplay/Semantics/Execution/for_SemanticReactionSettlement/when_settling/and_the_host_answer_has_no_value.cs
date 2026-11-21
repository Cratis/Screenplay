// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_settling;

public class and_the_host_answer_has_no_value : given.a_settlement_with_an_ability
{
    void Establish() => _answer = Answer(SemanticAbilityAnswerKind.Returned);
    async Task Because() => await SettleAsync();

    [Fact] void should_report_the_missing_result_as_unsupported() => _result.ShouldBeOfExactType<SemanticUnsupported>();
    [Fact] void should_keep_the_prior_world() => _result.World.ShouldEqual(_world);
}
