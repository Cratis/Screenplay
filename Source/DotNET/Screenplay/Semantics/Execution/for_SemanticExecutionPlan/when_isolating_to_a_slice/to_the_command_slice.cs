// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticExecutionPlan.when_isolating_to_a_slice;

public class to_the_command_slice : given.a_plan_with_two_slices
{
    SemanticExecutionPlan _isolated;

    void Because() => _isolated = _plan.IsolatedTo(_commandSlice.Id);

    [Fact] void should_record_the_slice() => _isolated.IsolatedSlice.ShouldEqual(_commandSlice.Id);
    [Fact] void should_run_the_slices_own_command() => _isolated.Runs(_commandSlice.Commands.Single().Id).ShouldBeTrue();
    [Fact] void should_not_run_the_other_slices_publication() => _isolated.Runs(_translationSlice.Projections.Single().Id).ShouldBeFalse();
    [Fact] void should_keep_every_projection_declared() => _isolated.Projections.Count.ShouldEqual(_plan.Projections.Count);
    [Fact] void should_keep_the_model_revision() => _isolated.Revision.ShouldEqual(_plan.Revision);
}
