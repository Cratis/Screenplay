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
    /// <param name="sources">All application sources, keyed by application-relative path, including imported documents.</param>
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
