// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Represents whether an event is a private local fact or a public contract other applications may consume.
/// </summary>
public enum SemanticEventVisibility
{
    /// <summary>A private local fact. This is the default and is never written to the canonical form.</summary>
    Private = 0,

    /// <summary>A public contract another application may rely on.</summary>
    Public = 1
}

/// <summary>
/// Represents the direction an explicitly directed translation faces.
/// </summary>
public enum SemanticTranslationDirection
{
    /// <summary>Turns outside occurrences or another application's public events into private local events.</summary>
    Inbound = 0,

    /// <summary>Turns private local events into exactly one local public event type.</summary>
    Outbound = 1
}

/// <summary>
/// Represents what a projection or reducer builds.
/// </summary>
public enum SemanticProjectionTargetKind
{
    /// <summary>A read model. This is the default and is never written to the canonical form.</summary>
    ReadModel = 0,

    /// <summary>The public event of an outbound translation: the folded state is the published event.</summary>
    Event = 1
}

/// <summary>
/// Represents the source of events a capture reads, as opposed to the rows of an outside system.
/// </summary>
/// <param name="Events">The declared event contracts, with an origin, whose occurrences are the captured items, in source order.</param>
public sealed record SemanticCaptureEventsSource(ImmutableArray<SemanticId> Events);
