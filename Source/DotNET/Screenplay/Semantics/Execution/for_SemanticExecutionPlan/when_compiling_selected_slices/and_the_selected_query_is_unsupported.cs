// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticExecutionPlan.when_compiling_selected_slices;

public class and_the_selected_query_is_unsupported : a_scoped_execution_model
{
    SemanticExecutionPlanCompilation _result;

    void Establish() => DeferQuery();
    void Because() => _result = SemanticExecutionPlan.Compile(_model, [_view.Id]);

    [Fact] void should_block_compilation() => _result.Success.ShouldBeFalse();
    [Fact] void should_not_create_a_plan() => _result.Plan.ShouldBeNull();
    [Fact] void should_report_the_selected_issue() => _result.Issues.Single().Artifact.ShouldEqual(_queryId);
    [Fact] void should_have_no_unselected_issues() => _result.UnselectedIssues.ShouldBeEmpty();
}
