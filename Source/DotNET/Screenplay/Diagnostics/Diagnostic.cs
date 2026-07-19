// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Diagnostics;

/// <summary>
/// Represents a message produced during compilation, tied to a location in the source text.
/// </summary>
/// <param name="Severity">The <see cref="DiagnosticSeverity"/> of the diagnostic.</param>
/// <param name="Message">The human readable message.</param>
/// <param name="Location">The <see cref="SourceLocation"/> the diagnostic refers to.</param>
public record Diagnostic(DiagnosticSeverity Severity, string Message, SourceLocation Location)
{
    /// <summary>
    /// Creates an error diagnostic.
    /// </summary>
    /// <param name="message">The human readable message.</param>
    /// <param name="location">The <see cref="SourceLocation"/> the diagnostic refers to.</param>
    /// <returns>A new <see cref="Diagnostic"/> with <see cref="DiagnosticSeverity.Error"/> severity.</returns>
    public static Diagnostic Error(string message, SourceLocation location) => new(DiagnosticSeverity.Error, message, location);

    /// <summary>
    /// Creates a warning diagnostic.
    /// </summary>
    /// <param name="message">The human readable message.</param>
    /// <param name="location">The <see cref="SourceLocation"/> the diagnostic refers to.</param>
    /// <returns>A new <see cref="Diagnostic"/> with <see cref="DiagnosticSeverity.Warning"/> severity.</returns>
    public static Diagnostic Warning(string message, SourceLocation location) => new(DiagnosticSeverity.Warning, message, location);
}
