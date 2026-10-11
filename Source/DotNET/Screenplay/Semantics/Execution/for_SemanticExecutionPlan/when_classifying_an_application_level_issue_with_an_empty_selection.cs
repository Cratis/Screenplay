// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics.Execution.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticExecutionPlan;

// All current validated concept rules have portable implementations or execution-time refusal.
// Pin empty-selection classification with an explicit application-level issue.
public class when_classifying_an_application_level_issue_with_an_empty_selection : a_scoped_execution_model
{
    bool _blocking;

    void Because() => _blocking = SemanticExecutionPlan.IsBlockingIssue(
        new(_projectNameConceptId, SemanticPlanIssueKind.UnsupportedValidation, "An application-level capability is unavailable."),
        ImmutableHashSet<SemanticId>.Empty,
        new Dictionary<SemanticId, SemanticId> { [_queryId] = _view.Id });

    [Fact] void should_block_the_empty_selection() => _blocking.ShouldBeTrue();
}
