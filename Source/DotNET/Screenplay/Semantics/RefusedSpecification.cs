// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Identifies a specification excluded from an opt-in compilation because its binding scope reported errors.
/// </summary>
/// <param name="Id">The authoritative semantic specification identity.</param>
/// <param name="Diagnostics">The original error diagnostics, without severity changes.</param>
/// <param name="Origin">The effective source specification and case origin, when expanded.</param>
public sealed record RefusedSpecification(SemanticId Id, ImmutableArray<Diagnostic> Diagnostics, EffectiveSpecification? Origin)
{
    internal ImmutableArray<SemanticId> DeclarationIds { get; init; } = [];
}
