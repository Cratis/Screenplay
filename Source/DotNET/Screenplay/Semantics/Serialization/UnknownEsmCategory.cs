// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Serialization;

/// <summary>
/// Classifies an unknown's known structural position, never its name or opaque contents.
/// </summary>
public enum UnknownEsmCategory
{
    /// <summary>No known security position surrounds the member.</summary>
    Unclassified,

    /// <summary>Authorization, policy, caller or claim semantics surround the member.</summary>
    Authorization,

    /// <summary>Identity mapping semantics surround the member.</summary>
    IdentityMapping,

    /// <summary>Compliance, personal-data or secret semantics surround the member.</summary>
    Compliance,

    /// <summary>Event-stream routing semantics surround the member.</summary>
    EventStreamRouting
}
