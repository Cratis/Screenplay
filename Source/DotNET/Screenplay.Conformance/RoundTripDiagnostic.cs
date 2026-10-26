// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

namespace Cratis.Screenplay.Conformance;

/// <summary>
/// Represents a diagnostic a round trip reported. Only the code is kept; messages and locations are provenance.
/// </summary>
/// <param name="Direction">The half of the round trip that reported it.</param>
/// <param name="Code">The diagnostic code.</param>
/// <param name="Severity">The severity.</param>
internal sealed record RoundTripDiagnostic(RoundTripDirection Direction, string Code, RoundTripDiagnosticSeverity Severity);
