// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Finds the source spans of a provided value's name and of its <c>$context</c> reads.
/// </summary>
internal static class WorkspaceProvideSpans
{
    const string ContextPrefix = "$context.";

    /// <summary>
    /// Decides whether a provide name or a <c>$context</c> read can be located on a line.
    /// </summary>
    /// <param name="node">The node holding the identifier.</param>
    /// <param name="member">The member of the node that holds the identifier.</param>
    /// <param name="line">The source line.</param>
    /// <returns>The decision, or <see langword="null"/> when the node and member are not a provide name or a context read.</returns>
    internal static bool? Supports(SyntaxNode node, string member, string line) => (node, member) switch
    {
        (ProvideSyntax, "name") => line.Split(' ', '\t')[0] == "provide",
        (ComparisonConditionSyntax, "left") or (ContextExpressionSyntax, "path") => line.Contains(ContextPrefix, StringComparison.Ordinal),
        _ => null
    };

    /// <summary>
    /// Finds the spans of a provide name or a <c>$context</c> read on a line.
    /// </summary>
    /// <param name="node">The node holding the identifier.</param>
    /// <param name="member">The member of the node that holds the identifier.</param>
    /// <param name="line">The source line.</param>
    /// <param name="expected">The identifier text expected at the span.</param>
    /// <returns>The spans, or <see langword="null"/> when the node and member are not a provide name or a context read.</returns>
    internal static IEnumerable<(int Offset, int Length)>? Find(SyntaxNode node, string member, string line, string expected) => (node, member) switch
    {
        (ComparisonConditionSyntax, "left") => ContextReads(line, expected, 0),
        (ContextExpressionSyntax, "path") => ContextReads(line, ContextPrefix + expected, ContextPrefix.Length),
        (ProvideSyntax, "name") => ProvideName(line, expected),
        _ => null
    };

    static IEnumerable<(int Offset, int Length)> ProvideName(string line, string expected)
    {
        var name = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries).ElementAtOrDefault(1);
        return name == expected ? [(line.IndexOf(name, "provide".Length, StringComparison.Ordinal), name.Length)] : [];
    }

    // A rename changes only the text after $context.; a read is the whole token so a longer name or path never matches by prefix.
    static IEnumerable<(int Offset, int Length)> ContextReads(string line, string token, int skip)
    {
        var comment = SourceLineSplitter.CommentStart(line);
        if (comment >= 0) line = line[..comment];

        for (var at = line.IndexOf(token, StringComparison.Ordinal); at >= 0; at = line.IndexOf(token, at + 1, StringComparison.Ordinal))
        {
            var end = at + token.Length;
            if (IsBoundaryBefore(line, at) && IsBoundaryAfter(line, end))
            {
                yield return (at + skip, token.Length - skip);
            }
        }
    }

    static bool IsBoundaryBefore(string line, int at) => at == 0 || !(char.IsLetterOrDigit(line[at - 1]) || line[at - 1] is '_' or '.' or '$');

    static bool IsBoundaryAfter(string line, int end) => end == line.Length || !(char.IsLetterOrDigit(line[end]) || line[end] is '_' or '.');
}
