// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticExecutionPlan.when_isolating_to_a_slice;

public class and_the_plan_is_not_isolated : given.a_plan_with_two_slices
{
    [Fact] void should_have_no_isolated_slice() => _plan.IsolatedSlice.ShouldBeNull();
    [Fact] void should_run_every_slices_behavior() => _plan.Runs(_translationSlice.Projections.Single().Id).ShouldBeTrue();
    [Fact] void should_find_the_slice_of_a_specification() => _plan.SliceOf(_translationSlice.Specifications.First().Id).ShouldEqual(_translationSlice.Id);
}
