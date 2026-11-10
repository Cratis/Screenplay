// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Words the warning for an action whose 'item' subject cannot be resolved.
/// </summary>
internal static class UnresolvedActionSubjectMessage
{
    /// <summary>
    /// Gets the message for the subjects that were in scope.
    /// </summary>
    /// <param name="subjects">The data subjects in scope.</param>
    /// <param name="guarded">Whether the action is a guarded action.</param>
    /// <returns>The warning text.</returns>
    internal static string For(IReadOnlyList<ScreenDataSyntax> subjects, bool guarded)
    {
        if (guarded) return "A guarded action requires exactly one nearest 'data' subject; no data or equally near data bindings leave 'item' unresolved";
        if (subjects.Count <= 1) return "Action 'for item' requires one resolvable data subject - no item binding is inferred";
        var candidates = string.Join(", ", subjects.Select(data => data.Type.IsCollection ? $"{data.Type.Name}[]" : data.Type.Name));
        return $"Action 'for item' is ambiguous between data {candidates} - keep exactly one data (or a table over exactly one collection data) in the action's container, or move the action into a section beside the data it acts on";
    }
}
