// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_committing_protected_reads;

public class with_a_read_race_and_a_late_uniqueness_claim : given.a_protected_command
{
    void Because()
    {
        var decision = _evaluator.Decide(_plan, _world, Request());
        _world = Append(_world, Fact("Recorded", "read", "changed"), Fact("Saved", "competitor", "original"));
        _result = decision.Commit(_world);
    }

    [Fact] void should_reject_the_constraint_instead_of_conflicting() => ((SemanticRejected)_result).Category.ShouldEqual(SemanticRejectionCategory.Constraint);
    [Fact] void should_name_the_constraint() => ((SemanticRejected)_result).Code.ShouldEqual("UniqueValue");
    [Fact] void should_keep_the_constraint_message() => ((SemanticRejected)_result).Details.ShouldEqual("Constraint 'UniqueValue' is violated: another event source already holds the constrained value.");
    [Fact] void should_preserve_the_commit_world() => _result.World.ShouldBeSame(_world);
    [Fact] void should_append_no_command_facts() => _result.World.Facts.Length.ShouldEqual(3);
}
