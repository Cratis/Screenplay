// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_committing_protected_reads;

public class with_a_late_uniqueness_claim : given.a_protected_command
{
    void Because()
    {
        var decision = _evaluator.Decide(_plan, _world, Request());
        _world = Append(_world, Fact("Saved", "competitor", "original"));
        _result = decision.Commit(_world);
    }

    [Fact] void should_enforce_uniqueness_against_the_latest_world() => ((SemanticRejected)_result).Category.ShouldEqual(SemanticRejectionCategory.Constraint);
    [Fact] void should_name_the_constraint() => ((SemanticRejected)_result).Code.ShouldEqual("UniqueValue");
    [Fact] void should_leave_the_intervening_claim_untouched() => _result.World.ShouldBeSame(_world);
    [Fact] void should_append_nothing() => _result.World.Facts.Length.ShouldEqual(2);
}
