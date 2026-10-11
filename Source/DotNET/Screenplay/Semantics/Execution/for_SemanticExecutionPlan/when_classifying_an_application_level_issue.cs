// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics.Execution.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticExecutionPlan;

// All current validated concept rules have portable implementations or execution-time refusal.
// Pin classification with an explicit issue so future application-level admission checks stay blocking.
public class when_classifying_an_application_level_issue : a_scoped_execution_model
{
    bool _blocking;

    void Because() => _blocking = SemanticExecutionPlan.IsBlockingIssue(
        new(_projectNameConceptId, SemanticPlanIssueKind.UnsupportedValidation, "An application-level capability is unavailable."),
        ImmutableHashSet.Create(_registration.Id),
        new Dictionary<SemanticId, SemanticId> { [_queryId] = _view.Id });

    [Fact] void should_always_block_the_selection() => _blocking.ShouldBeTrue();
}
