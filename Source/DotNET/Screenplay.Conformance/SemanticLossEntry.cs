// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.Collections.Immutable;

namespace Cratis.Screenplay.Conformance;

/// <summary>
/// Represents one accepted loss in a ledger.
/// </summary>
/// <param name="Fixture">The fixture the loss occurs in.</param>
/// <param name="Change">The kind of loss, or <see langword="null"/> when the ledger named an unknown kind.</param>
/// <param name="Path">The normalised path of a missing, unexpected or changed value.</param>
/// <param name="Direction">The half of the round trip that explains the loss, or <see langword="null"/> when the ledger named none.</param>
/// <param name="Diagnostics">The diagnostic codes that explain the loss.</param>
/// <param name="Issue">The issue tracking the loss, as <c language="text">Owner/Repo#N</c>.</param>
/// <param name="ByDesign">Why the loss is accepted by design.</param>
internal sealed record SemanticLossEntry(
    string Fixture,
    SemanticLossChange? Change,
    string? Path,
    RoundTripDirection? Direction,
    ImmutableArray<string> Diagnostics,
    string? Issue,
    string? ByDesign);
