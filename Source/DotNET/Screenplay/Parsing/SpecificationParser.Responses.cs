// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Parsing;

internal static partial class SpecificationParser
{
    static SpecificationReturnSyntax? ParseReturn(ParserContext context, SourceLine line)
    {
        var prefix = ThenReturnsPrefixRegex().Match(line.Content).Length;
        var value = line.Content[prefix..].Trim();
        if (value.Length > 0)
        {
            var concrete = ParseConcrete(context, value, line.LocationAt(line.Content.IndexOf(value, prefix, StringComparison.Ordinal)), DiagnosticCodes.InvalidReturnExpectation);
            if (context.TryPeekChild(line.Indent, out var child))
            {
                context.Error(DiagnosticCodes.InvalidReturnExpectation, "A scalar return expectation cannot have child assertions.", child.Location);
                context.SkipBlock(line.Indent);
            }

            return concrete is null ? null : new ScalarSpecificationReturnSyntax(concrete, line.Location);
        }

        var fields = new List<PropertyMappingSyntax>();
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (ParseConcreteMapping(context, child, MappingRegex(), DiagnosticCodes.InvalidReturnExpectation) is { } field)
            {
                fields.Add(field);
            }
        }

        return new RecordSpecificationReturnSyntax(fields, line.Location);
    }

    static PropertyMappingSyntax? ParseConcreteMapping(ParserContext context, SourceLine line, Regex regex, string code)
    {
        var match = regex.Match(line.Content);
        if (!match.Success)
        {
            context.Error(code, "Expected a named fixture or assertion with exactly one concrete value.", line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        var source = match.Groups[2];
        var value = ParseConcrete(context, source.Value, line.LocationAt(source.Index), code);
        if (context.TryPeekChild(line.Indent, out var child))
        {
            context.Error(code, "A fixture or return field cannot have child directives.", child.Location);
            context.SkipBlock(line.Indent);
        }

        return value is null ? null : new PropertyMappingSyntax(match.Groups[1].Value, value, line.Location);
    }

    static ExpressionSyntax? ParseConcrete(ParserContext context, string text, SourceLocation location, string code)
    {
        if (text.StartsWith("case.", StringComparison.Ordinal)) return ParseSpecificationValue(context, text, location);
        if (ContainsCaseReference(text)) context.Error(DiagnosticCodes.InvalidSpecificationCaseReference, "A case reference fills a whole value position; it cannot occur inside a structured value or expression.", location);

        // Existing literal parsing accepts any text between outer quotes. Check the whole token before
        // reusing it, so '"one" "two"' cannot masquerade as one string value.
        if ((text.StartsWith('"') || text.StartsWith('\'')) && !AbsentKeyStringRegex().IsMatch(text))
        {
            context.Error(code, "Expected exactly one concrete value.", location);
            return null;
        }

        var expression = ExpressionParser.ParseMappingSource(context, text, location);
        if (expression is not LiteralExpressionSyntax and not ObjectExpressionSyntax and not ListExpressionSyntax || (expression is LiteralExpressionSyntax { Value: double number } && !double.IsFinite(number)))
        {
            context.Error(code, "Expected exactly one concrete literal or structured value, not an expression.", location);
            return null;
        }

        return expression;
    }

    [GeneratedRegex(@"^then\s+returns\b", RegexOptions.None, 1000)]
    private static partial Regex ThenReturnsPrefixRegex();

    [GeneratedRegex(@"^generated\s+(?=\S)(?![=])", RegexOptions.None, 1000)]
    private static partial Regex GeneratedFixturePrefixRegex();

    [GeneratedRegex(@"^generated\s+([a-z_]\w*)\s*=(?!=|>)\s*(.+)$", RegexOptions.None, 1000)]
    private static partial Regex GeneratedFixtureRegex();
}
