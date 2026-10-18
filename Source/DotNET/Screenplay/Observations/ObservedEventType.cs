// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations;

/// <summary>
/// Identifies a stored event generation, not a Screenplay contract identity.
/// </summary>
/// <param name="TypeId">The stored type identifier.</param>
/// <param name="Generation">The stored generation.</param>
public sealed record ObservedEventType(string TypeId, uint Generation);

/// <summary>
/// Counts examined events of one generation.
/// </summary>
/// <param name="Type">The stored type and generation.</param>
/// <param name="Count">The event count, never an invocation count.</param>
public sealed record ObservedAppend(ObservedEventType Type, long Count);

/// <summary>
/// Counts events for which the host could not identify an innermost named producer.
/// </summary>
/// <param name="Type">The stored type and generation.</param>
/// <param name="Count">The unattributed event count.</param>
public sealed record UnattributedAppend(ObservedEventType Type, long Count);
