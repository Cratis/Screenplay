// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_validating_query_rejections : Specification
{
    SemanticApplication _application;
    SemanticSlice _slice;
    SemanticSpecification _specification;

    void Establish()
    {
        _application = query_validation_source.Compile().Value!.Model.Application;
        _slice = _application.Modules[0].Features[0].Slices[0];
        _specification = _slice.Specifications[0];
    }

    [Fact] void should_refuse_query_errors_in_v17() => Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V17, SemanticVersion.V17, _application)).ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_refuse_v18_without_a_query_error() => Reject(_specification with { ThenErrors = [] });
    [Fact] void should_refuse_denial_beside_an_error() => Reject(_specification with { ThenDenied = true });
    [Fact] void should_refuse_no_events_beside_an_error() => Reject(_specification with { ThenNoEvents = true });
    [Fact] void should_refuse_event_order_beside_an_error() => Reject(_specification with { ThenEventsInAnyOrder = true });
    [Fact] void should_refuse_two_errors() => Reject(_specification with { ThenErrors = [new(null, null), new(null, null)] });
    [Fact] void should_refuse_two_queries() => Reject(_specification with { ThenQueries = [_specification.ThenQueries[0], _specification.ThenQueries[0]] });
    [Fact] void should_refuse_an_actionless_error_without_a_query() => Reject(_specification with { ThenQueries = [] });
    [Fact] void should_refuse_success_results_beside_an_error() => Reject(_specification with { ThenQueries = [_specification.ThenQueries[0] with { Results = [new(_slice.ReadModels[0].Id, SemanticValue.Text("AB"), [])] }] });

    void Reject(SemanticSpecification specification)
    {
        var module = _application.Modules[0];
        var feature = module.Features[0];
        var application = _application with { Modules = [module with { Features = [feature with { Slices = [_slice with { Specifications = [specification] }] }] }] };
        Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V18, SemanticVersion.V18, application)).ShouldBeOfExactType<InvalidSemanticContract>();
    }
}
