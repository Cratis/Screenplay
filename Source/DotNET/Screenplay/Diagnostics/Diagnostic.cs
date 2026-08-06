// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Diagnostics;

/// <summary>
/// Represents a message produced during compilation, tied to a location in the source text.
/// </summary>
/// <param name="Severity">The <see cref="DiagnosticSeverity"/> of the diagnostic.</param>
/// <param name="Code">The stable <see cref="DiagnosticCodes">code</see> identifying the kind of diagnostic, for example <c>PLAY0001</c>.</param>
/// <param name="Message">The human readable message.</param>
/// <param name="Location">The <see cref="SourceLocation"/> the diagnostic refers to.</param>
/// <remarks>
/// The message is written for a reader and is reworded whenever a clearer wording is found; the code is what a
/// consumer suppresses, groups and reacts on. See <see cref="DiagnosticCodes"/> for the catalogue.
/// </remarks>
public record Diagnostic(DiagnosticSeverity Severity, string Code, string Message, SourceLocation Location)
{
    /// <summary>
    /// Creates an error diagnostic.
    /// </summary>
    /// <param name="code">The <see cref="DiagnosticCodes">code</see> identifying the kind of diagnostic.</param>
    /// <param name="message">The human readable message.</param>
    /// <param name="location">The <see cref="SourceLocation"/> the diagnostic refers to.</param>
    /// <returns>A new <see cref="Diagnostic"/> with <see cref="DiagnosticSeverity.Error"/> severity.</returns>
    public static Diagnostic Error(string code, string message, SourceLocation location) => new(DiagnosticSeverity.Error, code, message, location);

    /// <summary>
    /// Creates a warning diagnostic.
    /// </summary>
    /// <param name="code">The <see cref="DiagnosticCodes">code</see> identifying the kind of diagnostic.</param>
    /// <param name="message">The human readable message.</param>
    /// <param name="location">The <see cref="SourceLocation"/> the diagnostic refers to.</param>
    /// <returns>A new <see cref="Diagnostic"/> with <see cref="DiagnosticSeverity.Warning"/> severity.</returns>
    public static Diagnostic Warning(string code, string message, SourceLocation location) => new(DiagnosticSeverity.Warning, code, message, location);
}
