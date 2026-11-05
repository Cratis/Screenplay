// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Serialization;

/// <summary>
/// Promotes only the optional v37.1 read-model order member. The frozen .0 descriptors and
/// opaque extension encoding/order remain unchanged, including when this member becomes typed.
/// </summary>
internal static class EsmMinorMembers
{
    internal static bool IsPromoted(uint major, uint minor, EsmBaselineShape shape, string member) =>
        major == 37 && minor >= 1 && shape == EsmBaselineShape.ReadModel && member == "propertyOrder";
}
