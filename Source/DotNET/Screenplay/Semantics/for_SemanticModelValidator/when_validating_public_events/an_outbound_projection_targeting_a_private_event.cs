// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelValidator.when_validating_public_events;

public class an_outbound_projection_targeting_a_private_event : given.a_model_to_corrupt
{
    Exception _error;

    void Because()
    {
        var model = OutboundModel;
        var packed = model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).Single(slice => slice.Name == "PackOrder").Events.Single().Id;
        _error = Rebuild(model, "PublishOrderShipped", slice => slice with { Projections = [.. slice.Projections.Select(projection => projection with { ReadModel = packed })] });
    }

    [Fact] void should_refuse_the_model() => _error.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_say_it_may_target_only_its_own_public_event() => _error.Message.ShouldContain("may target only its own public event");
}
