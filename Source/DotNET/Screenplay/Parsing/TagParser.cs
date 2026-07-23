// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>tag</c> lines - static or dynamic tags on events, <c>produces</c> declarations and capture appends.
/// </summary>
internal static partial class TagParser
{
    /// <summary>
    /// Parses a tag from its already consumed line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="line">The consumed <see cref="SourceLine"/> holding the <c>tag</c> line.</param>
    /// <returns>The parsed <see cref="TagSyntax"/>, or <c>null</c> when the line is invalid.</returns>
    public static TagSyntax? Parse(ParserContext context, SourceLine line)
    {
        var value = line.Content["tag".Length..].Trim();
        if (value.Length == 0)
        {
            context.Error("Expected a value after 'tag' - an identifier, a string literal or a context expression", line.Location);
            return null;
        }

        if (IdentifierRegex().IsMatch(value))
        {
            return new(new LiteralExpressionSyntax(value, line.Location), line.Location);
        }

        var expression = ExpressionParser.ParseMappingSource(value, line.Location);
        if (expression is RawExpressionSyntax)
        {
            context.Error($"Invalid tag value '{value}' - expected an identifier, a string literal or a context expression", line.Location);
            return null;
        }

        return new(expression, line.Location);
    }

    [GeneratedRegex(@"^[A-Za-z_]\w*$", RegexOptions.None, 1000)]
    private static partial Regex IdentifierRegex();
}
