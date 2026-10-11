// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel.when_validating_reaction_identity.given;

public class a_model_without_v10_features : a_model_to_corrupt
{
    void Establish()
    {
        _application = WithIdentity(null);
        // The released v9 golden has precise, unused Key inputs that count as exact-number use at v10.
        // Normalize only these non-route inputs here, so this context has no v10 feature; leave the golden and route values unchanged.
        _application = WithNonRouteNumber(42m);
    }

    protected SemanticApplication WithNonRouteNumber(decimal number) => _application with
    {
        Modules = [.. _application.Modules.Select(module => module with
        {
            Features = [.. module.Features.Select(feature => feature with
            {
                Slices = [.. feature.Slices.Select(slice => slice.Name != "EventRoutes" ? slice : slice with
                {
                    Specifications = [.. slice.Specifications.Select(specification => specification with
                    {
                        When = specification.When is { } command ? command with
                        {
                            Values = [.. command.Values.Select(value => value.Value is SemanticNumberValue
                                ? value with { Value = SemanticValue.Number(specification.Name == "Route fixture 2" ? number : 42m) }
                                : value)]
                        } : null
                    })]
                })]
            })]
        })]
    };
}
