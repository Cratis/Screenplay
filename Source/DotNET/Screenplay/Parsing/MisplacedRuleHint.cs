// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Builds the guidance appended to a rejected <c>rule &lt;predicate&gt;</c> line that sits directly in a command or
/// concept body instead of inside a <c>validate</c> block.
/// </summary>
/// <remarks>
/// Only a line that reads like a declarative validation predicate gets a hint - <c>rule not empty</c>, <c>rule &gt; 0</c>.
/// A named <c>rule &lt;Name&gt;</c> and every other unknown directive keep the generic diagnostic. The hint never says the
/// suggested edit makes the application valid, and the parser still rejects the line.
/// </remarks>
internal static partial class MisplacedRuleHint
{
    /// <summary>
    /// Gets the hint for a command body line.
    /// </summary>
    /// <param name="content">The rejected line's content.</param>
    /// <returns>The hint to append to the diagnostic, or an empty string when the line is not a misplaced predicate.</returns>
    public static string ForCommand(string content) => Predicate(content) is { } rule
        ? $" - to validate a property, declare 'validate' and put '<property> {rule}' on an indented line beneath it"
        : string.Empty;

    /// <summary>
    /// Gets the hint for a concept body line.
    /// </summary>
    /// <param name="content">The rejected line's content.</param>
    /// <returns>The hint to append to the diagnostic, or an empty string when the line is not a misplaced predicate.</returns>
    public static string ForConcept(string content) => Predicate(content) is { } rule
        ? $". To validate the concept's value, declare 'validate' and put '{rule}' on an indented line beneath it"
        : string.Empty;

    static string? Predicate(string content)
    {
        var match = MisplacedRuleRegex().Match(content);
        return match.Success ? match.Groups[1].Value : null;
    }

    [GeneratedRegex(@"^rule\s+((?:not\s+empty|(?:not\s+contains|contains|starts\s+with|ends\s+with|matches|length|decimals|all|max|min)\s+\S.*|(?:>=|<=|==|!=|>|<)\s*\S.*))$", RegexOptions.NonBacktracking)]
    private static partial Regex MisplacedRuleRegex();
}
