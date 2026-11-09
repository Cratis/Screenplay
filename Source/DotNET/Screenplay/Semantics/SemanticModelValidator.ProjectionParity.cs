// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Version rules of the ESM v43 projection constructs: set-once mappings and the stored order of a children collection.
/// </summary>
internal static partial class SemanticModelValidator
{
    // ESM v43 constructs: a set-once projection mapping, or a children collection with a declared placement.
    internal static bool UsesProjectionParity(SemanticApplication application) =>
        SlicesOf(application).SelectMany(slice => slice.Projections).Any(projection =>
            ProjectionMappings(projection).Any(mapping => mapping.Operation == SemanticProjectionOperation.SetOnce) ||
            (projection.Scope is not null && OrdersChildren(projection.Scope)));

    static bool OrdersChildren(SemanticProjectionScope scope) =>
        scope.Children.Any(children => children.Ordering is not null || OrdersChildren(children.Scope)) ||
        scope.Nested.Any(nested => OrdersChildren(nested.Scope));

    static void ValidateProjectionParityVersion(SemanticApplication application, SemanticVersion version)
    {
        ValidateFeatureVersion(
            UsesProjectionParity(application),
            version,
            SemanticVersion.V43,
            "An ESM v43 model must declare a set-once projection mapping or an ordered children collection.",
            "Set-once projection mappings and ordered children collections require ESM v43.");
        ValidateChildrenOrderings(application);
    }

    static void ValidateChildrenOrderings(SemanticApplication application)
    {
        foreach (var projection in SlicesOf(application).SelectMany(slice => slice.Projections))
        {
            if (projection.Scope is not null) ValidateChildrenOrderings(projection.Scope);
        }
    }

    static void ValidateChildrenOrderings(SemanticProjectionScope scope)
    {
        foreach (var children in scope.Children)
        {
            if (children.Ordering is { } ordering)
            {
                if (ordering.Placement is not (SemanticChildrenPlacement.Prepend or SemanticChildrenPlacement.Sorted))
                {
                    throw new InvalidSemanticContract("A children ordering prepends or sorts; an appending children collection carries no ordering.");
                }

                if (ordering.Direction is not (SemanticSortDirection.Ascending or SemanticSortDirection.Descending))
                {
                    throw new InvalidSemanticContract("A children sort direction is ascending or descending.");
                }

                var sorted = ordering.Placement == SemanticChildrenPlacement.Sorted;
                if (sorted == ordering.SortBy.IsDefaultOrEmpty)
                {
                    throw new InvalidSemanticContract("Only a sorted children collection names the property it sorts by, and it always names one.");
                }

                if (ordering.Placement != SemanticChildrenPlacement.Sorted && ordering.Direction != SemanticSortDirection.Ascending)
                {
                    throw new InvalidSemanticContract("Only a sorted children collection carries a sort direction.");
                }
            }

            ValidateChildrenOrderings(children.Scope);
        }

        foreach (var nested in scope.Nested)
        {
            ValidateChildrenOrderings(nested.Scope);
        }
    }
}
