// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.Collections.Immutable;

namespace Cratis.Screenplay.Conformance;

/// <summary>
/// Represents what one fixture's round trip produced.
/// </summary>
/// <param name="Fixture">The fixture name, such as <c language="text">ProtectedCommandReadsCorpus.V25</c>.</param>
/// <param name="Comparison">The comparison of the expected and recovered models, or <see langword="null"/> when the recovered source did not compile.</param>
/// <param name="Diagnostics">The diagnostics both halves of the round trip reported.</param>
/// <param name="RecoveryCompileDiagnostics">The codes the compiler reported for the recovered source.</param>
internal sealed record RoundTripObservation(
    string Fixture,
    SemanticModelComparison? Comparison,
    ImmutableArray<RoundTripDiagnostic> Diagnostics,
    ImmutableArray<string> RecoveryCompileDiagnostics);
