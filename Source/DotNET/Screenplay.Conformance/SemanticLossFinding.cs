// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

namespace Cratis.Screenplay.Conformance;

/// <summary>
/// Represents one reason a reconciliation is not clean.
/// </summary>
/// <param name="Fixture">The fixture, or an empty string for a finding about the ledger or the run as a whole.</param>
/// <param name="Change">The kind of loss involved, when there is one.</param>
/// <param name="Path">The normalised path involved, when there is one.</param>
/// <param name="Message">What is wrong.</param>
internal sealed record SemanticLossFinding(string Fixture, SemanticLossChange? Change, string? Path, string Message);
