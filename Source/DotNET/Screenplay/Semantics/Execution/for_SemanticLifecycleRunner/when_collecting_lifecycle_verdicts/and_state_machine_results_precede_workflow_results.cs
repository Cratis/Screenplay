// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticLifecycleRunner.when_collecting_lifecycle_verdicts;

public class and_state_machine_results_precede_workflow_results : given.a_lifecycle_compilation
{
    ImmutableArray<SemanticLifecycleSpecificationResult> _results;

    void Because() => _results = SemanticLifecycleRunner.RunAll(Model);

    [Fact] void should_keep_machine_results_first() => _results.Select(result => result.Owner).ToArray().ShouldEqual(new[]
    {
        "InvoiceLifecycle", "InvoiceLifecycle", "Collect", "Collect", "Collect", "Collect", "Collect"
    });
}
