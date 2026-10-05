// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

/// <summary>
/// Finds the executable-model errors a proposal introduces, so an assistant fixes what it just wrote instead of
/// discovering it after apply. Errors the model already had are not repeated.
/// </summary>
static class McpIntroducedErrors
{
    internal const string Guidance =
        "This proposal introduces executable-model errors. Fix them in a new proposal before apply; apply as is only when the user accepts a model that does not run yet.";

    internal static IReadOnlyList<object> Between(ScreenplayWorkspace before, ScreenplayWorkspace after)
    {
        // A proposal moves lines, so an existing error is matched by code and message rather than by location.
        var existing = before.Compilation.Diagnostics
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .GroupBy(Key)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var introduced = new List<object>();
        foreach (var diagnostic in after.Compilation.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
        {
            if (existing.TryGetValue(Key(diagnostic), out var remaining) && remaining > 0)
            {
                existing[Key(diagnostic)] = remaining - 1;
                continue;
            }

            introduced.Add(new
            {
                code = diagnostic.Code,
                message = diagnostic.Message,
                path = diagnostic.Location.Path,
                line = diagnostic.Location.Line,
                column = diagnostic.Location.Column
            });
        }

        return introduced;
    }

    static string Key(Diagnostic diagnostic) => $"{diagnostic.Code}\n{diagnostic.Message}";
}
