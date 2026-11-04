// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator;

public class when_validating_an_ambiguous_published_fixture_from_every : given.a_published_history
{
    Exception _error;

    void Because()
    {
        var module = _model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slices = feature.Slices.Select(slice => slice with
        {
            Projections = [.. slice.Projections.Select(projection => projection.Target == SemanticProjectionTargetKind.Event
                ? projection with { Scope = projection.Scope! with { From = [], Every = new(true, false, projection.Scope.From.Single().Mappings) } }
                : projection)],
            Specifications = [.. slice.Specifications.Select(specification => specification with
            {
                GivenEvents = [new SemanticSpecificationEvent(_published.EventContract, _published.Values) { EventSource = _published.Context!.EventSource }]
            })]
        });
        _error = Catch.Exception(() => ExecutableSemanticModel.Create(
            _model.LanguageVersion,
            _model.SemanticVersion,
            _model.Application with { Modules = [module with { Features = [feature with { Slices = [.. slices] }] }] }));
    }

    [Fact] void should_refuse_the_ambiguous_unrouted_source_type() => _error.ShouldBeOfExactType<InvalidSemanticContract>();
}
