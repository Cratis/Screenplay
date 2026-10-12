// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.Execution.given;

public class a_scoped_execution_model : a_valid_semantic_model
{
    protected SemanticSlice _registration;
    protected SemanticSlice _view;
    protected SemanticExecutionPlan _plan;
    protected SemanticFact _fact;
    protected SemanticExecutionRequest _request;

    void Establish()
    {
        var slices = _application.Modules.Single().Features.Single().Slices;
        _registration = slices.Single(slice => slice.Commands.Length > 0);
        _view = slices.Single(slice => slice.Queries.Length > 0);
        var specification = _registration.Specifications.Single(specification => specification.Name == "registers a project");
        _fact = new(_eventId, specification.When!.Values[0].Value, specification.ThenEvents.Single().Values);
        _request = SemanticExecutionRequest.Create(_commandId, specification.When.Values, []);
    }

    protected void DeferQuery()
    {
        _model = ExecutableSemanticModel.Create(
            LanguageVersion.V1,
            SemanticVersion.V1,
            ReplaceSlice(_view with { Queries = [_view.Queries.Single() with { Cardinality = SemanticQueryCardinality.One }] }));
    }

    protected void DeferProjection()
    {
        var projection = _view.Projections.Single();
        _model = ExecutableSemanticModel.Create(
            LanguageVersion.V1,
            SemanticVersion.V1,
            ReplaceSlice(_view with
            {
                Projections = [projection with
                {
                    Transitions = [],
                    Scope = SemanticProjectionScope.Empty with
                    {
                        JoinRemovals = [new(_eventId, new SemanticProjectionValueKey(new SemanticProjectionEventProperty([_eventProjectIdPropertyId])))]
                    }
                }]
            }));
        _plan = SemanticExecutionPlan.Compile(_model, [_registration.Id]).Plan!;
    }
}
