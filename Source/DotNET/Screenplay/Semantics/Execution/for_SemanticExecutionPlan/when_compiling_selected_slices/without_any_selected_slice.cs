// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticExecutionPlan.when_compiling_selected_slices;

public class without_any_selected_slice : a_scoped_execution_model
{
    SemanticExecutionPlanCompilation _result;

    void Because() => _result = SemanticExecutionPlan.Compile(_model, []);

    [Fact] void should_block_compilation() => _result.Success.ShouldBeFalse();
    [Fact] void should_not_create_a_plan() => _result.Plan.ShouldBeNull();
    [Fact] void should_report_an_invalid_selection() => _result.Issues.Single().Kind.ShouldEqual(SemanticPlanIssueKind.InvalidSliceSelection);
    [Fact] void should_explain_the_empty_selection() => _result.Issues.Single().Details.ShouldContain("at least one slice");
}
