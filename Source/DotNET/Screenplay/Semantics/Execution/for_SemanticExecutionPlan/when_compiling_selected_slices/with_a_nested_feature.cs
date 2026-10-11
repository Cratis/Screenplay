// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticExecutionPlan.when_compiling_selected_slices;

public class with_a_nested_feature : a_scoped_execution_model
{
    SemanticExecutionPlanCompilation _result;

    void Establish()
    {
        DeferQuery();
        var module = _model.Application.Modules.Single();
        var feature = module.Features.Single();
        var nested = feature with { Id = Id(SemanticKind.Feature, "Nested"), Name = "Nested", Slices = [feature.Slices.Single(slice => slice.Id == _view.Id)] };
        _model = ExecutableSemanticModel.Create(
            LanguageVersion.V1,
            SemanticVersion.V1,
            _model.Application with { Modules = [module with { Features = [feature with { Slices = [_registration], Features = [nested] }] }] });
    }

    void Because() => _result = SemanticExecutionPlan.Compile(_model, [_registration.Id]);

    [Fact] void should_create_the_plan() => _result.Success.ShouldBeTrue();
    [Fact] void should_defer_the_nested_slice_issue() => _result.UnselectedIssues.Single().Artifact.ShouldEqual(_queryId);
}
