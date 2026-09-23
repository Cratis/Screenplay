// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Defines the outcome of resolving an <c>$eventContext.&lt;path&gt;</c> against the <see cref="EventContextCatalog"/>.
/// </summary>
public enum EventContextPathStatus
{
    /// <summary>
    /// Every segment names a member the catalog knows.
    /// </summary>
    Known = 0,

    /// <summary>
    /// The path is empty or has an empty segment, so there is nothing to resolve.
    /// </summary>
    Missing = 1,

    /// <summary>
    /// The first segment names no member of the event context.
    /// </summary>
    UnknownMember = 2,

    /// <summary>
    /// A later segment names nothing the member before it has.
    /// </summary>
    UnknownSubPath = 3,

    /// <summary>
    /// The path continues below a collection, which has no addressing grammar and never resolves.
    /// </summary>
    BelowCollection = 4
}
