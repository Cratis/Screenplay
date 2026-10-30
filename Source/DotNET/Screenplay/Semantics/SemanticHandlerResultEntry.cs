// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>Describes one possible event and its payload and destination contract.</summary>
/// <param name="EventContract">The event's stable identity.</param>
/// <param name="EventName">The event's declared name.</param>
/// <param name="EventRevision">The event's declared revision.</param>
/// <param name="Payload">The event payload shape.</param>
/// <param name="DestinationKind">The default, scalar or one-of destination kind.</param>
public sealed record SemanticHandlerResultEntry(SemanticId EventContract, string EventName, uint EventRevision, SemanticContextType Payload, string DestinationKind)
{
    /// <summary>Gets the command property supplying the destination, when explicitly declared.</summary>
    public string? DestinationProperty { get; init; }

    /// <summary>Gets the destination value type, or the element type for a one-of destination.</summary>
    public SemanticContextType? DestinationType { get; init; }
}
