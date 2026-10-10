// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel.when_validating_reaction_identity.given;

public class a_model_to_corrupt : Specification
{
    protected SemanticApplication _application;

    void Establish() => _application = canonical_serialization_golden_vectors.CreateReactionIdentityModelV10().Application;

    protected SemanticApplication WithIdentity(SemanticReactionIdentity? identity) => _application with
    {
        Modules = [.. _application.Modules.Select(module => module with
        {
            Features = [.. module.Features.Select(feature => feature with
            {
                Slices = [.. feature.Slices.Select(slice => slice with
                {
                    Reactions = [.. slice.Reactions.Select(reaction => reaction with { RunsAs = identity })]
                })]
            })]
        })]
    };
}
