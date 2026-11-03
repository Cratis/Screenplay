// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator;

public class when_validating_a_routed_published_fixture : given.a_published_history
{
    ExecutableSemanticModel _validated;
    SemanticSpecificationRun _result;

    void Because()
    {
        var identity = ApplicationIdentity.Create("Publication");
        var address = SemanticAddress.ForEventSource(identity, "Publication");
        var stream = new SemanticEventStream(SemanticId.Create(SemanticAddress.ForEventStream(address, "Published")), "Published", "published")
        {
            StreamIdType = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text)
        };
        var source = new SemanticEventSource(SemanticId.Create(address), "Publication", "publication", [stream]);
        var route = new SemanticFixtureRoute(source.Id, stream.Id) { StreamId = SemanticValue.Text("prices") };
        var module = _model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slices = feature.Slices.Select(slice => slice with
        {
            Specifications = [.. slice.Specifications.Select(specification => specification with
            {
                GivenEvents = [.. _live.Facts.Select(fact => new SemanticSpecificationEvent(fact.EventContract, fact.Values)
                {
                    EventSource = fact.Context!.EventSource,
                    Route = fact.EventContract == _published.EventContract ? route : null
                })]
            })]
        });
        _validated = ExecutableSemanticModel.Create(
            _model.LanguageVersion,
            _model.SemanticVersion,
            _model.Application with { EventSources = [source], Modules = [module with { Features = [feature with { Slices = [.. slices] }] }] });
        _result = new SemanticSpecificationRunner().Run(Plan(_validated), _plan.Specifications.Keys.Single());
    }

    [Fact] void should_accept_the_routed_published_source_type() => _validated.ShouldNotBeNull();
    [Fact] void should_execute_the_routed_published_fixture() => _result.Passed.ShouldBeTrue();
}
