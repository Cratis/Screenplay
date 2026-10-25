// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_committing_protected_reads;

public class with_a_read_race_and_a_constraint_settlement : given.a_protected_command
{
    SemanticRejected _settlement;
    int _settlementCalls;

    void Because()
    {
        var decided = _world;
        var dependencies = _evaluator.Decide(_plan, decided, Request()).Dependencies;
        _world = Append(decided, Fact("Recorded", "read", "changed"));
        _settlement = new(_world, SemanticRejectionCategory.Constraint, "ConstraintCode", "$strings.constraint") { MessageIsStringKey = true };
        var decision = new SemanticDecision(decided, dependencies, world =>
        {
            _settlementCalls++;
            world.ShouldBeSame(_world);
            return _settlement;
        });
        _result = decision.Commit(_world);
    }

    [Fact] void should_return_the_original_constraint_result_without_losing_metadata() => _result.ShouldBeSame(_settlement);
    [Fact] void should_settle_once() => _settlementCalls.ShouldEqual(1);
}
