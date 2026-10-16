// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_committing_protected_reads;

public class with_repeated_commits : given.a_protected_command
{
    SemanticDecision _decision;
    SemanticWorld _later;
    SemanticExecutionResult _first;
    SemanticExecutionResult _conflict;
    SemanticExecutionResult _again;
    SemanticDecisionDependencies _dependencies;

    void Because()
    {
        _decision = _evaluator.Decide(_plan, _world, Request());
        _dependencies = _decision.Dependencies;
        _later = Append(_world, Fact("Removed", "read"));
        _first = _decision.Commit(_world);
        _conflict = _decision.Commit(_later);
        _again = _decision.Commit(_world);
    }

    [Fact] void should_accept_against_the_decided_world() => _first.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_conflict_against_the_longer_world() => ((SemanticConflict)_conflict).Category.ShouldEqual(SemanticConflictCategory.DecisionStateChanged);
    [Fact] void should_carry_no_facts_in_the_conflict() => _conflict.World.ShouldBeSame(_later);
    [Fact] void should_accept_again_with_the_same_facts() => ((SemanticAccepted)_again).Facts.Single().Values.Single().Value.ShouldEqual(((SemanticAccepted)_first).Facts.Single().Values.Single().Value);
    [Fact] void should_not_change_the_decision() => (_decision.Boundary == 1 && ReferenceEquals(_decision.Dependencies, _dependencies)).ShouldBeTrue();
    [Fact] void should_not_change_either_world() => (_world.Facts.Length == 1 && _later.Facts.Length == 2).ShouldBeTrue();
}
