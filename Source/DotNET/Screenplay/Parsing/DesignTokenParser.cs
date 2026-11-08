// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Design;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses the <c>tokens</c> block of a design system.
/// </summary>
internal static partial class DesignTokenParser
{
    /// <summary>
    /// Parses a <c>tokens</c> block from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding <c>tokens</c>.</param>
    /// <param name="categories">The categories to add to.</param>
    public static void Parse(ParserContext context, SourceLine header, List<DesignTokenCategorySyntax> categories)
    {
        if (header.Content != "tokens")
        {
            context.Error(DiagnosticCodes.InvalidDesignSystemDeclaration, $"Invalid tokens line '{header.Content}' - expected 'tokens' followed by an indented block", header.Location);
        }

        while (context.TryPeekChild(header.Indent, out var categoryLine))
        {
            context.Reader.TakeSignificant();
            if (!DesignVocabulary.Categories.Contains(categoryLine.Content))
            {
                context.Error(DiagnosticCodes.InvalidDesignSystemDeclaration, $"Unknown token category '{categoryLine.Content}' - expected one of {string.Join(", ", DesignVocabulary.Categories)}", categoryLine.Location);
                context.SkipBlock(categoryLine.Indent);
                continue;
            }

            var tokens = new List<DesignTokenSyntax>();
            while (context.TryPeekChild(categoryLine.Indent, out var tokenLine))
            {
                context.Reader.TakeSignificant();
                context.SkipBlock(tokenLine.Indent);
                var match = TokenRegex().Match(tokenLine.Content);
                if (!match.Success)
                {
                    context.Error(DiagnosticCodes.InvalidDesignSystemDeclaration, $"Invalid token '{tokenLine.Content}' - expected '<name>' or '<name> \"<what it is for>\"'", tokenLine.Location);
                    continue;
                }

                var name = match.Groups["name"].Value;
                if (tokens.Exists(existing => existing.Name == name) || categories.Exists(existing => existing.Category == categoryLine.Content && existing.Tokens.Any(token => token.Name == name)))
                {
                    context.Error(DiagnosticCodes.DuplicateDesignToken, $"The token '{categoryLine.Content}.{name}' is already declared", tokenLine.Location);
                    continue;
                }

                var description = match.Groups["description"].Success ? StringLiteral.Unescape(match.Groups["description"].Value) : null;
                tokens.Add(new(name, description, tokenLine.Location));
            }

            categories.Add(new(categoryLine.Content, tokens, categoryLine.Location));
        }
    }

    [GeneratedRegex("^(?<name>[A-Za-z_][\\w\\-]*)(?:\\s+\"(?<description>" + StringLiteral.BodyPattern + ")\")?$", RegexOptions.NonBacktracking)]
    private static partial Regex TokenRegex();
}
