// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator;

public class when_deciding_a_read_only_request : when_committing_protected_reads.given.a_protected_command
{
    SemanticDecision _decision;

    void Because() => _decision = _evaluator.Decide(_plan, _world, SemanticExecutionRequest.ForQueries([]));

    [Fact] void should_refuse_with_a_contract_rejection() => ((SemanticRejected)_decision.Commit(_world)).Category.ShouldEqual(SemanticRejectionCategory.Contract);
    [Fact] void should_not_name_a_missing_command() => ((SemanticRejected)_decision.Commit(_world)).Details.ShouldNotContain("is not in the execution plan");
    [Fact] void should_depend_on_nothing() => _decision.Dependencies.Entries.ShouldBeEmpty();
    [Fact] void should_keep_the_decided_boundary() => _decision.Boundary.ShouldEqual(_world.Facts.Length);
}
