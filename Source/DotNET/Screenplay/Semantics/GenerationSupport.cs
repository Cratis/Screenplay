// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Numerics;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Describes generation support without applying property-scoped entropy diagnostics.
/// Prefixes, encoding and fixed dates add no random bits.
/// </summary>
/// <param name="Count">The exact minimum conditioned support count, or null when opaque.</param>
/// <param name="Bits">The display logarithm of the support count, or null when opaque.</param>
/// <param name="MeetsIdentifierMinimum">Whether exact support meets the catalog's identifier minimum; null when unknown.</param>
/// <param name="ValuesAtOnePercentCollision">The approximate draw count at one percent collision risk, or null when opaque; may be positive infinity for enormous support.</param>
public sealed record GenerationSupport(BigInteger? Count, double? Bits, bool? MeetsIdentifierMinimum, double? ValuesAtOnePercentCollision);
