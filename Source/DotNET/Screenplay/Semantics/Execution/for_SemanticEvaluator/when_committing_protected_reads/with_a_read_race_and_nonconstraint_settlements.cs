// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_committing_protected_reads;

public class with_a_read_race_and_nonconstraint_settlements : given.a_protected_command
{
    SemanticExecutionResult[] _results;

    void Because()
    {
        var decided = _world;
        var dependencies = _evaluator.Decide(_plan, decided, Request()).Dependencies;
        _world = Append(decided, Fact("Recorded", "read", "changed"));
        SemanticExecutionResult[] settlements =
        [
            new SemanticAccepted(_world, [], []),
            new SemanticUnsupported(_world, SemanticExecutionCapability.Projection, "Projection unavailable."),
            new SemanticConflict(_world, SemanticConflictCategory.DecisionStateChanged, "Another conflict."),
            .. Enum.GetValues<SemanticRejectionCategory>()
                .Where(category => category != SemanticRejectionCategory.Constraint)
                .Select(category => new SemanticRejected(_world, category, null, "Another rejection."))
        ];
        _results = [.. settlements.Select(settlement => new SemanticDecision(decided, dependencies, _ => settlement).Commit(_world))];
    }

    [Fact] void should_conflict_instead_of_accepting_or_reporting_another_failure() => _results.ShouldEachConformTo(result => result is SemanticConflict);
    [Fact] void should_keep_the_read_conflict_category() => _results.Cast<SemanticConflict>().ShouldEachConformTo(result => result.Category == SemanticConflictCategory.DecisionStateChanged);
    [Fact] void should_keep_the_read_conflict_message() => _results.Cast<SemanticConflict>().ShouldEachConformTo(result => result.Details == "Protected decision state changed after it was read.");
    [Fact] void should_preserve_the_commit_world() => _results.ShouldEachConformTo(result => ReferenceEquals(result.World, _world));
}
