// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Diagnostics;

/// <summary>
/// Classifies a permanent code that has no emission site. Emitting it requires removing this reservation.
/// </summary>
/// <param name="severity">The historical catalog severity.</param>
/// <param name="retired">Whether the diagnostic has been retired.</param>
[AttributeUsage(AttributeTargets.Field)]
public sealed class DiagnosticReservationAttribute(DiagnosticSeverity severity, bool retired = false) : Attribute
{
    /// <summary>
    /// Gets the historical catalog severity.
    /// </summary>
    public DiagnosticSeverity Severity { get; } = severity;

    /// <summary>
    /// Gets whether the code is retired.
    /// </summary>
    public bool Retired { get; } = retired;
}
