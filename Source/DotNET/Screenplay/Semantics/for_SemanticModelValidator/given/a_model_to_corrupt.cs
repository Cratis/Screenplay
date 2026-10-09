// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelValidator.given;

public class a_model_to_corrupt : for_SemanticModelBinder.given.a_public_events_model
{
    protected ExecutableSemanticModel OutboundModel => Bind(Outbound).Value!.Model;

    protected ExecutableSemanticModel InboundModel => Bind(Inbound).Value!.Model;

    protected static Exception Rebuild(ExecutableSemanticModel model, string slice, Func<SemanticSlice, SemanticSlice> change) =>
        Catch.Exception(() => ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, WithSlice(model.Application, slice, change)));

    protected static SemanticEventContract Public(ExecutableSemanticModel model, string slice) =>
        model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).Single(candidate => candidate.Name == slice).Events.First(@event => @event.Visibility == SemanticEventVisibility.Public);

    static SemanticApplication WithSlice(SemanticApplication application, string name, Func<SemanticSlice, SemanticSlice> change) =>
        application with
        {
            Modules = [.. application.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice.Name == name ? change(slice) : slice)]
                })]
            })]
        };
}
