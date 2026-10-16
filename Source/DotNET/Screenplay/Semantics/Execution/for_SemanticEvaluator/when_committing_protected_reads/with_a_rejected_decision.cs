// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_committing_protected_reads;

public class with_a_rejected_decision : given.a_protected_command
{
    void Because()
    {
        _world = World(Fact("Recorded", "read", string.Empty));
        var decision = _evaluator.Decide(_plan, _world, Request());
        _world = Append(_world, Fact("Recorded", "read", "now valid"));
        _result = decision.Commit(_world);
    }

    [Fact] void should_retain_the_decision_rejection_instead_of_conflicting() => ((SemanticRejected)_result).Category.ShouldEqual(SemanticRejectionCategory.Validation);
    [Fact] void should_preserve_the_requirement_message() => ((SemanticRejected)_result).Details.ShouldEqual("Value is empty.");
    [Fact] void should_append_nothing() => _result.World.ShouldBeSame(_world);
}
