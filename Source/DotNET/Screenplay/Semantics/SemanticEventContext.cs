// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Represents the typed identity of one event source at runtime.
/// </summary>
/// <param name="Type">The portable event-source identity type.</param>
/// <param name="Value">The concrete identity value.</param>
public sealed record SemanticEventSourceIdentity(
    SemanticTypeReference Type,
    SemanticValue Value);

/// <summary>
/// Represents portable context carried by one event occurrence.
/// </summary>
/// <param name="EventSource">The typed event-source identity.</param>
public sealed record SemanticEventContext(SemanticEventSourceIdentity EventSource);
