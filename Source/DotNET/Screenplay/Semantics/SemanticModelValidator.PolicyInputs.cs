// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    // ESM v46 construct: typed policy inputs and explicit input mappings.
    internal static bool UsesPolicyInputs(SemanticApplication application) =>
        application.Policies.Any(policy => !policy.Inputs.IsDefaultOrEmpty) || AuthorizationScopes(application).Any(HasMappings);

    static IEnumerable<SemanticAuthorization?> AuthorizationScopes(SemanticApplication application) =>
        application.Modules.SelectMany(module => module.Features).SelectMany(Slices).SelectMany(slice => slice.Commands.Select(command => command.Authorization).Concat(slice.Queries.Select(query => query.Authorization)));

    static IEnumerable<SemanticSlice> Slices(SemanticFeature feature) => feature.Slices.Concat(feature.Features.SelectMany(Slices));

    static bool HasMappings(SemanticAuthorization? authorization) => authorization switch
    {
        SemanticPolicyReference reference => !reference.Mappings.IsDefaultOrEmpty,
        SemanticLogicalAuthorization logical => HasMappings(logical.Left) || HasMappings(logical.Right),
        _ => false
    };

    static void ValidatePolicyInputVersion(SemanticApplication application, SemanticVersion version) =>
        ValidateFeatureVersion(
            UsesPolicyInputs(application),
            version,
            SemanticVersion.V46,
            "An ESM v46 model must declare a typed policy input or an input mapping.",
            "Typed policy inputs and input mappings require ESM v46.");
}
