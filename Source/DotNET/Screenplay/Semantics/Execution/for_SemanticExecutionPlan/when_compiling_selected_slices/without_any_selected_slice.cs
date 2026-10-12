// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticExecutionPlan.when_compiling_selected_slices;

public class without_any_selected_slice : a_scoped_execution_model
{
    SemanticExecutionPlanCompilation _result;

    void Because() => _result = SemanticExecutionPlan.Compile(_model, []);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_create_a_plan() => _result.Plan.ShouldNotBeNull();
    [Fact] void should_have_no_blocking_issues() => _result.Issues.ShouldBeEmpty();
    [Fact] void should_have_no_unselected_issues() => _result.UnselectedIssues.ShouldBeEmpty();
    [Fact] void should_keep_the_original_model() => ReferenceEquals(_result.Plan!.Model, _model).ShouldBeTrue();
    [Fact] void should_keep_the_original_revision() => _result.Plan!.Revision.ShouldEqual(_model.Revision);
}
