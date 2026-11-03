// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Resolves IANA time zone ids. A zone that cannot be found is never replaced by UTC (decision 0106).
/// </summary>
internal static class IanaTimeZones
{
    /// <summary>
    /// The IANA id of the zone used when a model declares none.
    /// </summary>
    internal const string Utc = "UTC";

    /// <summary>
    /// Gets whether an id is a known IANA time zone.
    /// </summary>
    /// <param name="id">The candidate id.</param>
    /// <returns><c>true</c> when the id names an IANA zone.</returns>
    internal static bool IsKnown(string? id) => TryFind(id, out _);

    /// <summary>
    /// Finds the zone for an IANA id.
    /// </summary>
    /// <param name="id">The IANA id.</param>
    /// <param name="zone">The zone when found.</param>
    /// <returns><c>true</c> when the id names an IANA zone.</returns>
    internal static bool TryFind(string? id, out TimeZoneInfo zone)
    {
        zone = TimeZoneInfo.Utc;
        if (string.IsNullOrWhiteSpace(id)) return false;
        if (id == Utc) return true;
        if (!TimeZoneInfo.TryFindSystemTimeZoneById(id, out var found) || !found.HasIanaId) return false;
        zone = found;
        return true;
    }
}
