// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses value expressions - both the host language mapping sources and the projection sub-language expressions.
/// </summary>
internal static partial class ExpressionParser
{
    /// <summary>
    /// Parses an expression as used inside a projection body.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <param name="text">The expression text.</param>
    /// <param name="location">The <see cref="SourceLocation"/> of the expression.</param>
    /// <returns>The parsed <see cref="ExpressionSyntax"/>.</returns>
    public static ExpressionSyntax ParseProjectionExpression(ParserContext context, string text, SourceLocation location)
    {
        text = text.Trim();

        if (text.StartsWith('`'))
        {
            return ParseTemplate(context, text, location);
        }

        if (text.StartsWith("literal ", StringComparison.Ordinal))
        {
            var literal = ParseLiteral(text["literal ".Length..].Trim(), location);
            if (literal is null)
            {
                context.Error($"Expected a literal value after 'literal', got '{text["literal ".Length..].Trim()}'", location);
                return new RawExpressionSyntax(text, location);
            }

            return literal;
        }

        if (text == "$eventSourceId")
        {
            return new EventSourceIdExpressionSyntax(location);
        }

        if (text.StartsWith("$eventContext.", StringComparison.Ordinal))
        {
            return new EventContextExpressionSyntax(text["$eventContext.".Length..], location);
        }

        if (text == "$causedBy")
        {
            return new CausedByExpressionSyntax(null, location);
        }

        if (text.StartsWith("$causedBy.", StringComparison.Ordinal))
        {
            var property = text["$causedBy.".Length..];
            if (property is not ("subject" or "name" or "username"))
            {
                context.Error($"Unknown $causedBy property '{property}' - expected subject, name or username", location);
            }

            return new CausedByExpressionSyntax(property, location);
        }

        if (ParseLiteral(text, location) is { } value)
        {
            return value;
        }

        if (PathRegex().IsMatch(text))
        {
            return new PathExpressionSyntax(text.Replace("@", string.Empty, StringComparison.Ordinal), location);
        }

        context.Error($"Invalid expression '{text}'", location);
        return new RawExpressionSyntax(text, location);
    }

    /// <summary>
    /// Parses a mapping source expression as used by <c>produces</c> and <c>capture</c> mappings.
    /// </summary>
    /// <param name="text">The expression text.</param>
    /// <param name="location">The <see cref="SourceLocation"/> of the expression.</param>
    /// <returns>The parsed <see cref="ExpressionSyntax"/>.</returns>
    public static ExpressionSyntax ParseMappingSource(string text, SourceLocation location)
    {
        text = text.Trim();

        if (text.StartsWith("$context.", StringComparison.Ordinal))
        {
            return new ContextExpressionSyntax(text["$context.".Length..], location);
        }

        if (text.StartsWith("$env.", StringComparison.Ordinal))
        {
            return new EnvironmentExpressionSyntax(text["$env.".Length..], location);
        }

        if (text.StartsWith("$.", StringComparison.Ordinal))
        {
            return new SourceItemExpressionSyntax(text["$.".Length..], location);
        }

        if (ParseLiteral(text, location) is { } literal)
        {
            return literal;
        }

        if (PathRegex().IsMatch(text))
        {
            return new PathExpressionSyntax(text, location);
        }

        return new RawExpressionSyntax(text, location);
    }

    /// <summary>
    /// Parses a literal value - a string, number, boolean or null.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="location">The <see cref="SourceLocation"/> of the literal.</param>
    /// <returns>The parsed <see cref="LiteralExpressionSyntax"/>, or <c>null</c> when the text is not a literal.</returns>
    public static LiteralExpressionSyntax? ParseLiteral(string text, SourceLocation location) => text switch
    {
        "true" => new(true, location),
        "false" => new(false, location),
        "null" => new(null, location),
        _ when text.Length >= 2 && text.StartsWith('"') && text.EndsWith('"') => new(text[1..^1], location),
        _ when text.Length >= 2 && text.StartsWith('\'') && text.EndsWith('\'') => new(text[1..^1], location),
        _ when NumberRegex().IsMatch(text) => new(double.Parse(text, CultureInfo.InvariantCulture), location),
        _ => null
    };

    static TemplateExpressionSyntax ParseTemplate(ParserContext context, string text, SourceLocation location)
    {
        var parts = new List<TemplatePartSyntax>();
        if (!text.EndsWith('`') || text.Length < 2)
        {
            context.Error("Unterminated template expression - expected a closing backtick", location);
            return new(parts, location);
        }

        var inner = text[1..^1];
        var textStart = 0;
        for (var i = 0; i < inner.Length; i++)
        {
            if (inner[i] == '$' && i + 1 < inner.Length && inner[i + 1] == '{')
            {
                if (i > textStart)
                {
                    parts.Add(new TemplateTextSyntax(inner[textStart..i], location));
                }

                var end = inner.IndexOf('}', i);
                if (end == -1)
                {
                    context.Error("Unterminated ${...} interpolation in template expression", location);
                    return new(parts, location);
                }

                var expression = ParseProjectionExpression(context, inner[(i + 2)..end], location);
                parts.Add(new TemplateInterpolationSyntax(expression, location));
                i = end;
                textStart = end + 1;
            }
        }

        if (textStart < inner.Length)
        {
            parts.Add(new TemplateTextSyntax(inner[textStart..], location));
        }

        return new(parts, location);
    }

    [GeneratedRegex(@"^@?[A-Za-z_]\w*(\.@?[A-Za-z_$]\w*)*$", RegexOptions.None, 1000)]
    private static partial Regex PathRegex();

    [GeneratedRegex(@"^-?\d+(\.\d+)?$", RegexOptions.None, 1000)]
    private static partial Regex NumberRegex();
}
