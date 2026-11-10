// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    // ESM v47 construct: a query that narrows its rows by a policy.
    internal static bool UsesNarrowing(SemanticApplication application) =>
        application.Modules.SelectMany(module => module.Features).SelectMany(Slices).SelectMany(slice => slice.Queries).Any(query => query.Narrowing is not null);

    static void ValidateNarrowingVersion(SemanticApplication application, SemanticVersion version)
    {
        ValidateFeatureVersion(
            UsesNarrowing(application),
            version,
            SemanticVersion.V47,
            "An ESM v47 model must declare a query that narrows its rows by a policy.",
            "A query that narrows its rows by a policy requires ESM v47.");
        foreach (var query in application.Modules.SelectMany(module => module.Features).SelectMany(Slices).SelectMany(slice => slice.Queries).Where(query => query.Narrowing is not null))
        {
            if (query.Cardinality != SemanticQueryCardinality.Many) throw new InvalidSemanticContract($"Query '{query.Name}' narrows rows but does not return a collection.");
            if (application.Policies.All(policy => policy.Name != query.Narrowing!.Name)) throw new InvalidSemanticContract($"Query '{query.Name}' narrows by an unresolved policy '{query.Narrowing!.Name}'.");
        }
    }
}
