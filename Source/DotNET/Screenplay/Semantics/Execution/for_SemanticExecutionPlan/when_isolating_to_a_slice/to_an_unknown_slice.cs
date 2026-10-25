// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticExecutionPlan.when_isolating_to_a_slice;

public class to_an_unknown_slice : given.a_plan_with_two_slices
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => _plan.IsolatedTo(SemanticId.Create(SemanticKind.Slice, "missing")));

    [Fact] void should_refuse_it() => _error.ShouldBeOfExactType<InvalidSemanticContract>();
}
