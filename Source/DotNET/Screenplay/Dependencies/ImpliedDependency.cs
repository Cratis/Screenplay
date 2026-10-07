// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies;

/// <summary>
/// Aggregated dependency between disjoint nodes at requested levels.
/// </summary>
/// <param name="Source">The consuming node.</param>
/// <param name="Target">The producing node.</param>
/// <param name="SliceEdges">Distinct consumer/producer slice pairs, independent of kind.</param>
/// <param name="References">Total references.</param>
/// <param name="ByKind">Reference counts by kind, in kind order.</param>
/// <param name="Consumers">Distinct consuming slices in authored order.</param>
/// <param name="Producers">Distinct producing slices or contexts in authored order.</param>
/// <param name="Evidence">The bounded ordered evidence.</param>
/// <param name="EvidenceCount">The full evidence count before capping.</param>
/// <param name="EvidenceTruncated">Whether evidence was capped.</param>
public sealed record ImpliedDependency(DependencyNode Source, DependencyNode Target, int SliceEdges, int References, IReadOnlyDictionary<string, int> ByKind, IReadOnlyList<DependencyNode> Consumers, IReadOnlyList<DependencyNode> Producers, IReadOnlyList<DependencyEvidence> Evidence, int EvidenceCount, bool EvidenceTruncated);
