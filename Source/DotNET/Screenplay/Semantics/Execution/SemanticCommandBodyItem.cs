// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Supplies one selected event and its payload in a result-v2 handler batch.
/// </summary>
/// <param name="EventContract">The selected possible event's stable identity.</param>
/// <param name="Payload">The event's semantic composite payload.</param>
/// <param name="Destination">The selected member for a one-of destination; otherwise null.</param>
public sealed record SemanticCommandBodyItem(SemanticId EventContract, SemanticValue Payload, SemanticValue? Destination = null);
