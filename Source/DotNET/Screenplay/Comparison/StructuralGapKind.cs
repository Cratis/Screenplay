// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison;

enum StructuralGapKind
{
    IncompleteSource,
    ExampleResolution,
    NotComparableAssigned,
    NotComparableIndexed,
    AddressKeysOnly,
    UnassignedEventContracts,
    UnassignedSpecifications,
    UnresolvedDependants,
    IdentitiesNotCompared,
    DeclarationLevelOnly
}
