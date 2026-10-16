// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Represents a key assembled from the read model's top-level identifier parts, without an authored composite type.
/// </summary>
/// <param name="Parts">The parts, whose order has no meaning.</param>
public sealed record SemanticProjectionPartsKey(ImmutableArray<SemanticProjectionKeyPart> Parts) : SemanticProjectionKey(SemanticProjectionKeyKind.Parts);
