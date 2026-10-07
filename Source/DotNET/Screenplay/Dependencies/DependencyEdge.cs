// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies;

/// <summary>
/// A slice dependency oriented consumer to producer, with all explicit references retained.
/// </summary>
/// <param name="Consumer">The consuming slice.</param>
/// <param name="Producer">The producing slice or outside context.</param>
/// <param name="Kind">The dependency kind.</param>
/// <param name="Evidence">Ordered references behind this edge.</param>
internal sealed record DependencyEdge(DependencyNode Consumer, DependencyNode Producer, string Kind, IReadOnlyList<DependencyEvidence> Evidence);
