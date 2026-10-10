// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    static void ValidateReactionIdentityVersion(SemanticApplication application, SemanticVersion version)
    {
        var identities = application.Modules.SelectMany(module => module.Features).SelectMany(AllSlices)
            .SelectMany(slice => slice.Reactions).Select(reaction => reaction.RunsAs).OfType<SemanticReactionIdentity>().ToArray();
        if (!version.IsAtLeast(SemanticVersion.V10) && identities.Length > 0)
        {
            throw new InvalidSemanticContract("A reaction system identity requires ESM v10.");
        }

        foreach (var identity in identities)
        {
            if (identity.Kind != SemanticReactionIdentityKind.System || identity.Roles.IsDefault ||
                identity.Roles.Any(string.IsNullOrWhiteSpace) || identity.Roles.Distinct(StringComparer.Ordinal).Count() != identity.Roles.Length ||
                !identity.Roles.SequenceEqual(identity.Roles.Order(StringComparer.Ordinal)))
            {
                throw new InvalidSemanticContract("A reaction identity must be system with non-blank, distinct, ordinally sorted roles.");
            }
        }
    }
}
