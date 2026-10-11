// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticExecutionPlan.when_compiling_selected_slices;

public class with_a_command_identity_instead_of_a_slice : a_scoped_execution_model
{
    SemanticExecutionPlanCompilation _result;

    void Because() => _result = SemanticExecutionPlan.Compile(_model, [_commandId]);

    [Fact] void should_block_compilation() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_an_invalid_selection() => _result.Issues.Single().Kind.ShouldEqual(SemanticPlanIssueKind.InvalidSliceSelection);
    [Fact] void should_not_defer_the_selection_issue() => _result.UnselectedIssues.ShouldBeEmpty();
}
