// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Screenplay.Completeness;

namespace Cratis.Screenplay.Mcp;

public static partial class ScopedDiagnostics
{
    /// <summary>
    /// Compiles the whole application and selects diagnostics for a scope and its direct dependents.
    /// </summary>
    /// <param name="sources">All application sources, keyed by application-relative path; every key is a compilation root.</param>
    /// <param name="scope">The case-sensitive dotted module, feature or slice address.</param>
    /// <param name="checks">The completeness checks to run when the whole application has no source errors.</param>
    /// <param name="result">The immutable selection when the scope is uniquely resolved; otherwise null.</param>
    /// <param name="error">The unknown or ambiguous scope outcome on failure; otherwise null.</param>
    /// <returns>Whether the scope was uniquely resolved, not whether its diagnostics are error-free.</returns>
    /// <remarks>
    /// Diagnostics include complete direct-dependent declarations, but not transitive dependents.
    /// Whole-application counts include the selected completeness findings. An error-free selection
    /// does not prove that the whole application is valid or executable.
    /// </remarks>
    public static bool TryValidate(
        IReadOnlyDictionary<string, string> sources,
        string scope,
        CompletenessChecks checks,
        [NotNullWhen(true)] out ScopedDiagnosticResult? result,
        [NotNullWhen(false)] out ScopeSelectionError? error) =>
        TryValidate(new McpSnapshot(sources), scope, checks, out result, out error);

    /// <summary>
    /// Compiles a file and its imports, or a folder as one application, and selects scoped diagnostics.
    /// </summary>
    /// <param name="path">An existing .play entry file or an application folder.</param>
    /// <param name="scope">The case-sensitive dotted module, feature or slice address.</param>
    /// <param name="checks">The completeness checks to run when the whole application has no source errors.</param>
    /// <param name="result">The immutable selection when the path is readable and the scope uniquely resolves; otherwise null.</param>
    /// <param name="error">The invalid path, unreadable path, unknown scope or ambiguous scope outcome; otherwise null.</param>
    /// <returns>Whether compilation could run and the scope uniquely resolved, not whether its diagnostics are error-free.</returns>
    /// <remarks>
    /// Uses the same file discovery, import roots and encoding handling as the screenplay tool.
    /// Missing paths and I/O or permission failures are returned as errors rather than thrown.
    /// </remarks>
    public static bool TryValidate(
        string path,
        string scope,
        CompletenessChecks checks,
        [NotNullWhen(true)] out ScopedDiagnosticResult? result,
        [NotNullWhen(false)] out ScopeSelectionError? error)
    {
        result = null;
        var isFile = File.Exists(path);
        if (!isFile && !Directory.Exists(path))
        {
            error = new(ScopeSelectionErrorKind.InvalidPath, $"'{path}' does not exist");
            return false;
        }
        if (isFile && !path.EndsWith(".play", StringComparison.OrdinalIgnoreCase))
        {
            error = new(ScopeSelectionErrorKind.InvalidPath, $"'{path}' is not a .play file");
            return false;
        }

        try
        {
            return TryValidate(McpSnapshot.Compile(path, isFile), scope, checks, out result, out error);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            error = new(ScopeSelectionErrorKind.UnreadablePath, $"Could not check '{path}': {exception.Message}");
            return false;
        }
    }

    internal static bool TryValidate(
        McpSnapshot snapshot,
        string scope,
        CompletenessChecks checks,
        [NotNullWhen(true)] out ScopedDiagnosticResult? result,
        [NotNullWhen(false)] out ScopeSelectionError? error)
    {
        result = SelectScope(snapshot, scope, snapshot.Completeness(checks), out error);

        return result is not null;
    }
}
