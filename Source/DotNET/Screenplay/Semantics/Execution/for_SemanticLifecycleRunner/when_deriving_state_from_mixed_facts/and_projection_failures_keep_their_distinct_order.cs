// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_lifecycles.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticLifecycleRunner.when_deriving_state_from_mixed_facts;

public class and_projection_failures_keep_their_distinct_order : given.a_lifecycle_compilation
{
    ImmutableArray<SemanticLifecycleSpecificationResult> _results;

    void Establish() => Bind(a_lifecycle_source.Source.Replace("status = \"sent\"", "status = $eventContext.hash", StringComparison.Ordinal));

    void Because() => _results = SemanticLifecycleRunner.RunAll(Model);

    [Fact] void should_keep_one_projection_problem_before_expectation_failures() => _results[1].Failures.ToArray().ShouldEqual(new[]
    {
        "The facts cannot be projected: Projection 'InvoiceProjection' was not admitted by scoped execution-plan compilation.",
        "Expected state 'Overdue', but 'InvoiceLifecycle' is in 'Sent'."
    });
}
