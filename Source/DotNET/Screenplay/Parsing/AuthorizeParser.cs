// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>authorize</c> declarations with their policy references and continuation lines.
/// </summary>
internal static partial class AuthorizeParser
{
    /// <summary>
    /// Parses an <c>authorize</c> declaration from its already consumed first line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="line">The consumed <see cref="SourceLine"/> holding the <c>authorize</c> keyword.</param>
    /// <returns>The parsed <see cref="AuthorizeSyntax"/>.</returns>
    public static AuthorizeSyntax Parse(ParserContext context, SourceLine line)
    {
        var policies = new List<PolicyReferenceSyntax>();
        AddReferences(context, policies, line.Content["authorize".Length..], line);

        while (context.TryPeekChild(line.Indent, out var child) && ContinuationRegex().IsMatch(child.Content))
        {
            context.Reader.TakeSignificant();
            AddReferences(context, policies, child.Content, child);
        }

        if (policies.Count == 0)
        {
            context.Error("Expected at least one policy after 'authorize'", line.Location);
        }

        return new(policies, line.Location);
    }

    static void AddReferences(ParserContext context, List<PolicyReferenceSyntax> policies, string text, SourceLine line)
    {
        var isAlternative = false;
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (word == "or")
            {
                isAlternative = true;
                continue;
            }

            if (!NameRegex().IsMatch(word))
            {
                context.Error($"Invalid policy reference '{word}' - policy names are PascalCase identifiers", line.Location);
                continue;
            }

            policies.Add(new(word, isAlternative, line.Location));
            isAlternative = false;
        }
    }

    [GeneratedRegex(@"^(?:or\s+)?[A-Z]\w*(?:\s+or\s+[A-Z]\w*)*$", RegexOptions.None, 1000)]
    private static partial Regex ContinuationRegex();

    [GeneratedRegex(@"^[A-Z]\w*$", RegexOptions.None, 1000)]
    private static partial Regex NameRegex();
}
