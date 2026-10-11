// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticExecutionPlan.when_compiling_selected_slices;

public class with_an_empty_selection_and_unsupported_slices : a_scoped_execution_model
{
    SemanticExecutionPlanCompilation _result;
    SemanticExecutionPlanCompilation _wholeModel;

    void Establish()
    {
        var projection = _view.Projections.Single();
        _model = ExecutableSemanticModel.Create(
            LanguageVersion.V1,
            SemanticVersion.V1,
            ReplaceSlice(_view with
            {
                Queries = [_view.Queries.Single() with { Cardinality = SemanticQueryCardinality.One }],
                Projections = [projection with
                {
                    Transitions = [],
                    Scope = SemanticProjectionScope.Empty with
                    {
                        JoinRemovals = [new(_eventId, new SemanticProjectionValueKey(new SemanticProjectionEventProperty([_eventProjectIdPropertyId])))]
                    }
                }]
            }));
        _wholeModel = SemanticExecutionPlan.Compile(_model);
    }

    void Because() => _result = SemanticExecutionPlan.Compile(_model, []);

    [Fact] void should_create_the_plan() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_blocking_issues() => _result.Issues.ShouldBeEmpty();
    [Fact] void should_defer_every_slice_issue() => _result.UnselectedIssues.ShouldContainOnly(_wholeModel.Issues.ToArray());
    [Fact] void should_report_both_unsupported_declarations() => _result.UnselectedIssues.Length.ShouldEqual(2);
    [Fact] void should_keep_the_original_model() => ReferenceEquals(_result.Plan!.Model, _model).ShouldBeTrue();
    [Fact] void should_keep_the_original_revision() => _result.Plan!.Revision.ShouldEqual(_model.Revision);
    [Fact] void should_keep_deferred_declarations_indexed() => _result.Plan!.Queries.ContainsKey(_queryId).ShouldBeTrue();
    [Fact] void should_still_block_whole_model_compilation() => _wholeModel.Success.ShouldBeFalse();
}
