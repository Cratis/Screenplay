// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ScreenCompositionCorpus;

/// <summary>
/// Separates the 0110 action subject advisories (PLAY0818/PLAY0819) from the frozen published corpus diagnostics.
/// </summary>
static class ActionSubjectAdvisories
{
    /// <summary>
    /// Determines whether a diagnostic is an action subject advisory the compiler reports but the frozen corpus does not pin.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to inspect.</param>
    /// <returns>True when the diagnostic is an inferred or changed action subject advisory.</returns>
    public static bool IsAdvisory(Diagnostic diagnostic) =>
        string.Equals(diagnostic.Code, DiagnosticCodes.InferredActionSubject, StringComparison.Ordinal) ||
        string.Equals(diagnostic.Code, DiagnosticCodes.ChangedActionSubject, StringComparison.Ordinal);

    /// <summary>
    /// Gets the diagnostics that belong to the frozen corpus boundary.
    /// </summary>
    /// <param name="diagnostics">The compile diagnostics.</param>
    /// <returns>The diagnostics without action subject advisories.</returns>
    public static Diagnostic[] WithoutAdvisories(IEnumerable<Diagnostic> diagnostics) => [.. diagnostics.Where(diagnostic => !IsAdvisory(diagnostic))];

    /// <summary>
    /// Gets the action subject advisories as ordinally sorted "code: message" lines.
    /// </summary>
    /// <param name="diagnostics">The compile diagnostics.</param>
    /// <returns>The sorted advisory lines.</returns>
    public static string[] Describe(IEnumerable<Diagnostic> diagnostics) => [.. diagnostics.Where(IsAdvisory).Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}").Order(StringComparer.Ordinal)];

    /// <summary>
    /// Gets the action subject advisories as ordinally sorted "code@line action" lines.
    /// </summary>
    /// <param name="diagnostics">The compile diagnostics.</param>
    /// <returns>The sorted advisory locations.</returns>
    public static string[] DescribeLocations(IEnumerable<Diagnostic> diagnostics) => [.. diagnostics.Where(IsAdvisory).Select(diagnostic => $"{diagnostic.Code}@{diagnostic.Location.Line} {ActionName(diagnostic)}").Order(StringComparer.Ordinal)];

    /// <summary>
    /// Gets the source line an advisory points at.
    /// </summary>
    /// <param name="diagnostic">The advisory.</param>
    /// <param name="documents">The documents of the compiled source form, as display path, stable key and text.</param>
    /// <returns>The source line, or null when the location does not resolve to a document line.</returns>
    public static string? SourceLineOf(Diagnostic diagnostic, IReadOnlyList<(string DisplayPath, string StableKey, string Text)> documents)
    {
        var document = documents.Count == 1 && diagnostic.Location.Path is null
            ? documents[0]
            : documents.FirstOrDefault(candidate => string.Equals(candidate.DisplayPath, diagnostic.Location.Path, StringComparison.Ordinal) || string.Equals(candidate.StableKey, diagnostic.Location.Path, StringComparison.Ordinal));
        if (document.Text is null)
        {
            return null;
        }

        var lines = document.Text.Split('\n');
        return diagnostic.Location.Line >= 1 && diagnostic.Location.Line <= lines.Length ? lines[diagnostic.Location.Line - 1] : null;
    }

    /// <summary>
    /// Gets the action an advisory names.
    /// </summary>
    /// <param name="diagnostic">The advisory.</param>
    /// <returns>The action name quoted first in the message.</returns>
    public static string ActionName(Diagnostic diagnostic)
    {
        var start = diagnostic.Message.IndexOf('\'', StringComparison.Ordinal) + 1;
        return diagnostic.Message[start..diagnostic.Message.IndexOf('\'', start)];
    }
}
