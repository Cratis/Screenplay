// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics.Generation;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Describes a standalone declaration assessment, not whole-model admission or runtime readiness.
/// </summary>
/// <param name="Concept">The inspected semantic concept, possibly partial when findings report errors, or null when binding could not inspect it.</param>
/// <param name="Findings">Construct-targeted compiler findings.</param>
/// <param name="Proofs">Generation-rule proofs parallel to Concept.Validations when generation was assessable; otherwise empty.</param>
/// <param name="Support">Exact generation support, or null for non-generated or invalid declarations. An opaque strategy has a support object with null members.</param>
/// <param name="RequiredVersion">The minimum ESM version needed by the inspected concept alone, not the enclosing application.</param>
public sealed record SemanticConceptCheckResult(
    SemanticConcept? Concept,
    ImmutableArray<SemanticConceptFinding> Findings,
    ImmutableArray<RuleConsistencyResult> Proofs,
    GenerationSupport? Support,
    SemanticVersion RequiredVersion);
