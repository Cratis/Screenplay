// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_committing_protected_reads;

public class with_irrelevant_intervening_state : given.a_protected_command
{
    SemanticFact _intervening;

    void Establish() => _intervening = Fact("OtherRecorded", "read", "intervening");

    void Because()
    {
        var decision = _evaluator.Decide(_plan, _world, Request());
        _result = decision.Commit(Append(_world, _intervening));
    }

    [Fact] void should_accept() => _result.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_preserve_the_intervening_fact() => _result.World.Facts[1].ShouldBeSame(_intervening);
    [Fact] void should_preserve_its_projection() => _result.World.ReadModels.Single(instance => instance.ReadModel == _plan.ReadModels.Values.Single(view => view.Name == "OtherView").Id).Values.ShouldContain(value => value.Value == SemanticValue.Text("intervening"));
    [Fact] void should_keep_the_decided_mapping() => ((SemanticAccepted)_result).Facts.Single().Values.Single().Value.ShouldEqual(SemanticValue.Text("original"));
}
