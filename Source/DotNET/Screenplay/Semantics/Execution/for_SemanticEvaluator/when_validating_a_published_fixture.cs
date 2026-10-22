// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator;

public class when_validating_a_published_fixture : given.a_published_history
{
    ExecutableSemanticModel _validated;

    void Because()
    {
        var module = _model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slices = feature.Slices.Select(slice => slice with
        {
            Specifications = [.. slice.Specifications.Select(specification => specification with
            {
                GivenEvents = [.. _live.Facts.Select(fact => new SemanticSpecificationEvent(fact.EventContract, fact.Values) { EventSource = fact.Context!.EventSource })]
            })]
        }).ToImmutableArray();
        var application = _model.Application with { Modules = [module with { Features = [feature with { Slices = slices }] }] };
        _validated = ExecutableSemanticModel.Create(_model.LanguageVersion, _model.SemanticVersion, application);
    }

    [Fact] void should_accept_the_published_source_type() => _validated.ShouldNotBeNull();
    [Fact] void should_execute_the_published_fixture() => new SemanticSpecificationRunner().Run(Plan(_validated), _plan.Specifications.Keys.Single()).Passed.ShouldBeTrue();
}
