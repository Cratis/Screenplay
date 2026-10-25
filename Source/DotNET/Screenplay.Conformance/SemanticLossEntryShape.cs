// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Cratis.Screenplay.Conformance;

/// <summary>
/// Checks the shape of ledger entries (R5).
/// </summary>
internal static partial class SemanticLossEntryShape
{
    /// <summary>
    /// Finds every problem with the shape of a ledger's entries.
    /// </summary>
    /// <param name="entries">The entries, in ledger order.</param>
    /// <returns>The problems, each naming the entry.</returns>
    public static IEnumerable<string> Problems(IReadOnlyList<SemanticLossEntry> entries)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            var where = $"Loss {index.ToString(CultureInfo.InvariantCulture)} ({entry.Fixture})";
            foreach (var problem in Problems(entry))
            {
                yield return $"{where}: {problem}";
            }

            if (!seen.Add($"{entry.Fixture}|{entry.Change}|{entry.Path}"))
            {
                yield return $"{where}: the entry duplicates another entry for the same fixture, change and path.";
            }
        }
    }

    /// <summary>
    /// Checks whether a change names a value at a path.
    /// </summary>
    /// <param name="change">The change.</param>
    /// <returns><see langword="true"/> for missing, unexpected and changed; otherwise, <see langword="false"/>.</returns>
    public static bool IsAtPath(SemanticLossChange? change) =>
        change is SemanticLossChange.Missing or SemanticLossChange.Unexpected or SemanticLossChange.Changed;

    static IEnumerable<string> Problems(SemanticLossEntry entry)
    {
        if (entry.Direction is null)
        {
            yield return "the entry must name a direction, 'render' or 'adapter'.";
        }

        if (IsAtPath(entry.Change) && string.IsNullOrWhiteSpace(entry.Path))
        {
            yield return "a missing, unexpected or changed entry must name a path.";
        }

        if (entry.Path?.Contains('*', StringComparison.Ordinal) == true)
        {
            yield return "the path must not contain a wildcard.";
        }

        if ((entry.Issue is null) == (entry.ByDesign is null))
        {
            yield return "the entry must carry exactly one of 'issue' and 'byDesign'.";
        }

        if (entry.Issue is not null && !IssuePattern().IsMatch(entry.Issue))
        {
            yield return $"the issue '{entry.Issue}' is not of the form 'Owner/Repo#N'.";
        }

        if (entry.ByDesign is not null && string.IsNullOrWhiteSpace(entry.ByDesign))
        {
            yield return "'byDesign' must give a reason.";
        }
    }

    [GeneratedRegex("^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+#[1-9][0-9]*$", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex IssuePattern();
}
