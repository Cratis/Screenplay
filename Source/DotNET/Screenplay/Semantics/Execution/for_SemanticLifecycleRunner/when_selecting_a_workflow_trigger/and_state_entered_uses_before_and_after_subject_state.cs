// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticLifecycleRunner.when_selecting_a_workflow_trigger;

public class and_state_entered_uses_before_and_after_subject_state : given.a_lifecycle_compilation
{
    ImmutableArray<SemanticLifecycleSpecificationResult> _results;

    void Because() => _results = SemanticLifecycleRunner.RunAll(Model);

    [Fact] void should_start_when_the_subject_enters_overdue() => _results.Single(result => result.Name == "APaidInvoiceIsTagged").Failures.ShouldBeEmpty();
}
