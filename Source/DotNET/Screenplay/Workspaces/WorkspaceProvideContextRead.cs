// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// A <c>$context.&lt;name&gt;</c> read found in a syntax entry, split into the member that holds it and the path it reads.
/// </summary>
/// <param name="Entry">The syntax entry holding the read.</param>
/// <param name="Member">The member of the entry that holds the read.</param>
/// <param name="Path">The context path without the <c>$context.</c> prefix.</param>
/// <param name="Prefix">The text in front of the path inside the member.</param>
sealed record WorkspaceProvideContextRead(WorkspaceSyntaxEntry Entry, string Member, string Path, string Prefix)
{
    const string ContextPrefix = "$context.";

    internal string Root => Path.Split('.')[0];

    internal static IEnumerable<WorkspaceProvideContextRead> In(WorkspaceSyntaxIndex index)
    {
        foreach (var entry in index.Entries)
        {
            switch (entry.Node)
            {
                case ContextExpressionSyntax expression:
                    yield return new(entry, "path", expression.Path, string.Empty);
                    break;
                case ComparisonConditionSyntax comparison when comparison.Left.StartsWith(ContextPrefix, StringComparison.Ordinal):
                    yield return new(entry, "left", comparison.Left[ContextPrefix.Length..], ContextPrefix);
                    break;
            }
        }
    }

    internal static bool Mentions(string text, string name)
    {
        var token = ContextPrefix + name;
        for (var at = text.IndexOf(token, StringComparison.Ordinal); at >= 0; at = text.IndexOf(token, at + 1, StringComparison.Ordinal))
        {
            if (EndsToken(text, at + token.Length))
            {
                return true;
            }
        }

        return false;
    }

    internal string Renamed(string replacement) => $"{Prefix}{replacement}{Path[Root.Length..]}";

    static bool EndsToken(string text, int end) => end == text.Length || !IsIdentifierCharacter(text[end]);

    static bool IsIdentifierCharacter(char character) => char.IsLetterOrDigit(character) || character == '_';
}
