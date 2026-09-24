// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses value expressions - both the host language mapping sources and the projection sub-language expressions.
/// </summary>
internal static partial class ExpressionParser
{
    const string CausedByMember = "causedBy";

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
                context.Error(DiagnosticCodes.ExpectedLiteralValue, $"Expected a literal value after 'literal', got '{text["literal ".Length..].Trim()}'", location);
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
            var path = text["$eventContext.".Length..];
            EventContextPathValidator.Validate(context, path, location);
            return new EventContextExpressionSyntax(path, location);
        }

        if (text == "$causedBy")
        {
            return new CausedByExpressionSyntax(null, location);
        }

        if (text.StartsWith("$causedBy.", StringComparison.Ordinal))
        {
            // The short form reads the same identity as $eventContext.causedBy, so it admits what the catalog lists below it.
            var property = text["$causedBy.".Length..];
            var resolution = EventContextCatalog.Resolve($"{CausedByMember}.{property}");
            if (!resolution.IsKnown)
            {
                context.Error(
                    DiagnosticCodes.UnknownCausedByProperty,
                    resolution.Expected.Count == 0
                        ? $"Unknown $causedBy property '{property}' - '{resolution.Member?.Name}' has no members"
                        : $"Unknown $causedBy property '{property}' - expected {string.Join(", ", resolution.Expected.Select(member => member.Name))}",
                    location);
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

        context.Error(DiagnosticCodes.InvalidExpression, $"Invalid expression '{text}'", location);
        return new RawExpressionSyntax(text, location);
    }

    /// <summary>
    /// Parses a mapping source expression as used by <c>produces</c> and <c>capture</c> mappings.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <param name="text">The expression text.</param>
    /// <param name="location">The <see cref="SourceLocation"/> of the expression.</param>
    /// <returns>The parsed <see cref="ExpressionSyntax"/>.</returns>
    public static ExpressionSyntax ParseMappingSource(ParserContext context, string text, SourceLocation location)
    {
        text = text.Trim();

        if (text.StartsWith("$context.", StringComparison.Ordinal))
        {
            var expression = new ContextExpressionSyntax(text["$context.".Length..], location);
            WarnOnUnknownContextPath(context, expression);
            return expression;
        }

        if (text.StartsWith("$env.", StringComparison.Ordinal))
        {
            return new EnvironmentExpressionSyntax(text["$env.".Length..], location);
        }

        if (text.StartsWith("$strings.", StringComparison.Ordinal))
        {
            return new StringsExpressionSyntax(text["$strings.".Length..], location);
        }

        if (text.StartsWith("$.", StringComparison.Ordinal))
        {
            return new SourceItemExpressionSyntax(text["$.".Length..], location);
        }

        if (text.StartsWith('{') || text.StartsWith('['))
        {
            try
            {
                var expression = new StructuredValueParser(text, location, context).Parse();
                if (expression is ObjectExpressionSyntax or ListExpressionSyntax)
                {
                    return expression;
                }
            }
            catch (Exception exception) when (exception is JsonException or InvalidStructuredNumber)
            {
                context.Error(DiagnosticCodes.InvalidStructuredValue, $"Invalid inline structured value: {exception.Message}", location);
                return new RawExpressionSyntax(text, location);
            }

            context.Error(DiagnosticCodes.InvalidStructuredValue, "Expected a JSON object or list value", location);
            return new RawExpressionSyntax(text, location);
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
    /// Parses a <c>property = source</c> mapping line, recording the exact source spans of the right-hand side.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <param name="property">The target property.</param>
    /// <param name="source">The regular expression group capturing the right-hand source text on <paramref name="line"/>.</param>
    /// <param name="line">The <see cref="SourceLine"/> the mapping is declared on.</param>
    /// <returns>The parsed <see cref="PropertyMappingSyntax"/> with server-owned source spans.</returns>
    public static PropertyMappingSyntax ParseMapping(ParserContext context, string property, Group source, SourceLine line)
    {
        var text = source.Value.Trim();
        var start = line.LocationAt(source.Index + (source.Value.Length - source.Value.TrimStart().Length));
        var expression = ParseMappingSource(context, text, text.StartsWith('{') || text.StartsWith('[') ? start : line.Location);
        if (expression is LiteralExpressionSyntax literal)
        {
            expression = literal with { RawLocation = start, RawLength = text.Length };
        }

        return new(property, expression, line.Location)
        {
            SourceLocation = start,
            SourceLength = text.Length
        };
    }

    /// <summary>
    /// Parses a projection mapping's source, retaining the exact span of a literal for workspace edits.
    /// </summary>
    /// <param name="context">The parser context.</param>
    /// <param name="source">The regular expression group containing the source.</param>
    /// <param name="line">The authored source line.</param>
    /// <returns>The parsed projection expression.</returns>
    public static ExpressionSyntax ParseProjectionMappingSource(ParserContext context, Group source, SourceLine line)
    {
        var text = source.Value.Trim();
        var expression = ParseProjectionExpression(context, text, line.Location);
        if (expression is LiteralExpressionSyntax literal)
        {
            // 'literal ' is projection syntax, not part of the raw literal value.
            var prefix = text.StartsWith("literal ", StringComparison.Ordinal) ? text.Length - text["literal ".Length..].TrimStart().Length : 0;
            var offset = source.Index + (source.Value.Length - source.Value.TrimStart().Length) + prefix;
            expression = literal with { RawLocation = line.LocationAt(offset), RawLength = text.Length - prefix };
        }

        return expression;
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
        _ when text.Length >= 2 && text.StartsWith('"') && text.EndsWith('"') => new(StringLiteral.Unescape(text[1..^1]), location),
        _ when text.Length >= 2 && text.StartsWith('\'') && text.EndsWith('\'') => new(StringLiteral.Unescape(text[1..^1]), location),
        _ when NumberRegex().IsMatch(text) => new(double.Parse(text, CultureInfo.InvariantCulture), location),
        _ => null
    };

    /// <summary>
    /// Warns when a <c>$context.</c> path does not name something the command or query context carries.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to report the diagnostic to.</param>
    /// <param name="expression">The <see cref="ContextExpressionSyntax"/> to check.</param>
    /// <remarks>
    /// The path is the declarative half of the same context a handler or performer compiles against, so a
    /// path outside it can never resolve at runtime. It stays a warning rather than an error - a runtime is
    /// free to expose more than the language names.
    /// </remarks>
    static void WarnOnUnknownContextPath(ParserContext context, ContextExpressionSyntax expression)
    {
        if (!ContextExpressionSyntax.KnownRoots.Contains(expression.Root))
        {
            context.Warning(
                DiagnosticCodes.UnknownContextPath,
                $"Unknown $context path '{expression.Path}' - expected one of {string.Join(", ", ContextExpressionSyntax.KnownRoots)}",
                expression.Location);
            return;
        }

        var segments = expression.Path.Split('.');
        if (segments.Length < 2)
        {
            return;
        }

        if (expression.Root == "causedBy" && !ContextExpressionSyntax.KnownCausedByProperties.Contains(segments[1]))
        {
            context.Warning(
                DiagnosticCodes.UnknownContextCausedByProperty,
                $"Unknown $context.causedBy property '{segments[1]}' - expected {string.Join(", ", ContextExpressionSyntax.KnownCausedByProperties)}",
                expression.Location);
        }

        // Anything under 'claims' is the name of a claim rather than a member, so only the segment naming
        // the member itself is checked against what the identity carries.
        if (expression.Root == "identity" && !ContextExpressionSyntax.KnownIdentityProperties.Contains(segments[1]))
        {
            context.Warning(
                DiagnosticCodes.UnknownContextIdentityProperty,
                $"Unknown $context.identity property '{segments[1]}' - expected {string.Join(", ", ContextExpressionSyntax.KnownIdentityProperties)}",
                expression.Location);
        }
    }

    static TemplateExpressionSyntax ParseTemplate(ParserContext context, string text, SourceLocation location)
    {
        var parts = new List<TemplatePartSyntax>();
        if (!text.EndsWith('`') || text.Length < 2)
        {
            context.Error(DiagnosticCodes.UnterminatedTemplateExpression, "Unterminated template expression - expected a closing backtick", location);
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
                    context.Error(DiagnosticCodes.UnterminatedInterpolation, "Unterminated ${...} interpolation in template expression", location);
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
