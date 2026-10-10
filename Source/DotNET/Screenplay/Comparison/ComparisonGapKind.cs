// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison;

/// <summary>
/// Defines ComparisonGapKind values for structural model comparison.
/// </summary>
public enum ComparisonGapKind
{
    /// <summary>
    /// Syntax or import placement is incomplete.
    /// </summary>
    IncompleteSource,

    /// <summary>
    /// Effective specification example resolution failed.
    /// </summary>
    ExampleResolution,

    /// <summary>
    /// Assigned identities lack unique comparable authored members.
    /// </summary>
    NotComparableAssigned,

    /// <summary>
    /// Indexed groups lack comparable authored members.
    /// </summary>
    NotComparableIndexed,

    /// <summary>
    /// Unassigned declarations use exact authoring keys only.
    /// </summary>
    AddressKeysOnly,

    /// <summary>
    /// Unassigned event contracts lack persisted contract continuity.
    /// </summary>
    UnassignedEventContracts,

    /// <summary>
    /// Unassigned specifications lack persisted identity continuity.
    /// </summary>
    UnassignedSpecifications,

    /// <summary>
    /// Dependency references are unresolved, ambiguous or incomplete.
    /// </summary>
    UnresolvedDependants,

    /// <summary>
    /// Address matching does not compare persisted identities.
    /// </summary>
    IdentitiesNotCompared,

    /// <summary>
    /// At least one input is not executable; both use authoring declaration keys.
    /// </summary>
    DeclarationLevelOnly
}
