// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Classifies a concept's value. Personal classification takes precedence over secret realization.
/// </summary>
/// <param name="Personal">The personal-data marker, or null when absent.</param>
/// <param name="Secret">The operational-secret marker, or null when absent.</param>
public sealed record SemanticConceptCompliance(SemanticPersonalData? Personal, SemanticSecret? Secret);

/// <summary>
/// Classifies personal data without recording free-text reasons or processing purposes.
/// </summary>
/// <param name="Special">The single GDPR Article 9 category, or null when absent.</param>
/// <param name="Criminal">Whether the value concerns criminal convictions and offenses.</param>
public sealed record SemanticPersonalData(string? Special = null, bool Criminal = false);

/// <summary>
/// Classifies an operational secret without enrolling it in personal-data erasure.
/// </summary>
/// <param name="Scope">The explicit encryption scope, or null for the provider default.</param>
public sealed record SemanticSecret(string? Scope = null);
