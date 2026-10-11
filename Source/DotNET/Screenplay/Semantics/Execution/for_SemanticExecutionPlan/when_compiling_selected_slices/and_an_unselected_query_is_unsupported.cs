// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticExecutionPlan.when_compiling_selected_slices;

public class and_an_unselected_query_is_unsupported : a_scoped_execution_model
{
    SemanticExecutionPlanCompilation _result;

    void Establish() => DeferQuery();
    void Because() => _result = SemanticExecutionPlan.Compile(_model, [_registration.Id, _registration.Id]);

    [Fact] void should_create_the_plan() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_blocking_issues() => _result.Issues.ShouldBeEmpty();
    [Fact] void should_report_the_unselected_query() => _result.UnselectedIssues.Single().Artifact.ShouldEqual(_queryId);
    [Fact] void should_report_the_query_capability() => _result.UnselectedIssues.Single().Kind.ShouldEqual(SemanticPlanIssueKind.UnsupportedQuery);
    [Fact] void should_keep_the_original_model() => ReferenceEquals(_result.Plan!.Model, _model).ShouldBeTrue();
    [Fact] void should_keep_the_original_revision() => _result.Plan!.Revision.ShouldEqual(_model.Revision);
    [Fact] void should_keep_unselected_declarations_indexed() => _result.Plan!.Queries.ContainsKey(_queryId).ShouldBeTrue();
}
