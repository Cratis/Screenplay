// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

internal static partial class SpecificationParser
{
    internal static SpecificationParameterSyntax? ParseParameter(ParserContext context, SourceLine line)
    {
        var offset = line.Content.IndexOf(' ') + 1;
        var property = offset > 0 ? PropertyLineParser.Parse(context, line with { Content = line.Content[offset..], Indent = line.Indent + offset }) : null;
        if (property?.IsGenerated != false || property.IsIdentifier)
        {
            context.Error(DiagnosticCodes.InvalidSpecificationParameter, "Expected 'parameter <name> <Type>', with optional collection and optional modifiers.", line.Location);
            SkipBody(context, line.Indent);
            return null;
        }
        if (context.TryPeekChild(line.Indent, out var child))
        {
            context.Error(DiagnosticCodes.InvalidSpecificationParameter, "A parameter has no body or default.", child.Location);
            SkipBody(context, line.Indent);
        }

        return new(property.Name, property.Type, line.Location);
    }

    internal static SpecificationCaseSyntax? ParseCase(ParserContext context, SourceLine line)
    {
        var match = CaseHeaderRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidSpecificationCase, "Expected 'case <Name>' with at most one inline parameter assignment.", line.Location);
            SkipBody(context, line.Indent);
            return null;
        }
        var values = new List<PropertyMappingSyntax>();
        if (match.Groups["property"].Success)
        {
            var source = match.Groups["value"];
            var value = ParseCaseConcrete(context, source.Value, line.LocationAt(source.Index));
            if (value is not null) values.Add(new(match.Groups["property"].Value, value, line.LocationAt(match.Groups["property"].Index)));
        }
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            var assignment = MappingRegex().Match(child.Content);
            if (!assignment.Success)
            {
                context.Error(DiagnosticCodes.InvalidSpecificationCaseAssignment, "Expected '<parameter> = <concrete value>' in a case.", child.Location);
                SkipBody(context, child.Indent);
                continue;
            }
            var source = assignment.Groups[2];
            var value = ParseCaseConcrete(context, source.Value, child.LocationAt(source.Index));
            if (value is not null) values.Add(new(assignment.Groups[1].Value, value, child.Location));
            if (context.TryPeekChild(child.Indent, out var nested))
            {
                context.Error(DiagnosticCodes.InvalidSpecificationCaseValue, "A case value must fit on one line.", nested.Location);
                SkipBody(context, child.Indent);
            }
        }

        return new(match.Groups[1].Value, values, line.Location);
    }

    internal static ExpressionSyntax? ParseCaseConcrete(ParserContext context, string text, SourceLocation location)
    {
        if (text.StartsWith("case.", StringComparison.Ordinal))
        {
            context.Error(DiagnosticCodes.InvalidSpecificationCaseValue, "A case assigns concrete values, not case references.", location);
            return null;
        }

        return ParseConcrete(context, text, location, DiagnosticCodes.InvalidSpecificationCaseValue);
    }

    internal static bool ContainsCaseReference(string text) => CaseTokenRegex().Matches(text).Any(match => match.Groups["reference"].Success);

    internal static ExpressionSyntax ParseSpecificationValue(ParserContext context, string text, SourceLocation location)
    {
        if (!text.StartsWith("case.", StringComparison.Ordinal))
        {
            if (ContainsCaseReference(text)) context.Error(DiagnosticCodes.InvalidSpecificationCaseReference, "A case reference fills a whole value position; it cannot occur inside a structured value or expression.", location);
            return ExpressionParser.ParseMappingSource(context, text, location);
        }
        var match = CaseReferenceRegex().Match(text);
        if (!match.Success) context.Error(DiagnosticCodes.InvalidSpecificationCaseReference, "A case reference fills a whole value position: 'case.<parameter>'.", location);

        return new CaseValueExpressionSyntax(match.Success ? match.Groups[1].Value : text[5..], location);
    }

    internal static PropertyMappingSyntax ParseSpecificationMapping(ParserContext context, string property, Group source, SourceLine line)
    {
        var mapping = ExpressionParser.ParseMapping(context, property, source, line);
        if (!source.Value.TrimStart().StartsWith("case.", StringComparison.Ordinal) && ContainsCaseReference(source.Value)) context.Error(DiagnosticCodes.InvalidSpecificationCaseReference, "A case reference fills a whole value position; it cannot occur inside a structured value or expression.", line.LocationAt(source.Index));
        return source.Value.TrimStart().StartsWith("case.", StringComparison.Ordinal)
            ? mapping with { Source = ParseSpecificationValue(context, source.Value.Trim(), line.LocationAt(source.Index)) } : mapping;
    }

    internal static void ValidateTable(SpecificationSyntax specification, ParserContext context)
    {
        var parameters = specification.Parameters.ToArray();
        var cases = specification.Cases.ToArray();
        if (parameters.Length == 0 != (cases.Length == 0)) context.Error(DiagnosticCodes.IncompleteSpecificationTable, "A specification table requires parameters and at least one case.", specification.Location);
        foreach (var duplicate in parameters.GroupBy(parameter => parameter.Name).SelectMany(group => group.Skip(1))) context.Error(DiagnosticCodes.DuplicateSpecificationParameter, $"Parameter '{duplicate.Name}' is declared more than once.", duplicate.Location);
        foreach (var duplicate in cases.GroupBy(row => row.Name).SelectMany(group => group.Skip(1))) context.Error(DiagnosticCodes.DuplicateSpecificationCase, $"Case '{duplicate.Name}' is declared more than once.", duplicate.Location);
        foreach (var row in cases)
        {
            foreach (var value in row.Values.Where(value => !parameters.Any(parameter => parameter.Name == value.Property))) context.Error(DiagnosticCodes.InvalidSpecificationCaseAssignment, $"Case '{row.Name}' assigns undeclared parameter '{value.Property}'.", value.Location);
            foreach (var parameter in parameters.Where(parameter => row.Values.Count(value => value.Property == parameter.Name) != 1)) context.Error(DiagnosticCodes.InvalidSpecificationCaseAssignment, $"Case '{row.Name}' must assign parameter '{parameter.Name}' exactly once.", row.Location);
        }
        var references = new CaseReferences();
        references.VisitSpecification(specification);
        foreach (var reference in references.Values)
        {
            if (!parameters.Any(parameter => parameter.Name == reference.Parameter) || cases.Length == 0) context.Error(DiagnosticCodes.InvalidSpecificationCaseReference, $"Case reference '{reference.Parameter}' requires a declared parameter in a specification table.", reference.Location);
        }
        foreach (var parameter in parameters.Where(parameter => !references.Values.Exists(reference => reference.Parameter == parameter.Name))) context.Warning(DiagnosticCodes.UnusedSpecificationParameter, $"Parameter '{parameter.Name}' is never referenced.", parameter.Location);
        if (specification.WhenRedelivered is { } locator)
        {
            var excluded = new CaseReferences();
            excluded.VisitSpecificationRedelivery(locator);
            foreach (var reference in excluded.Values) context.Error(DiagnosticCodes.InvalidSpecificationCaseReference, "Case references are not permitted in redelivery locators.", reference.Location);
        }
    }

    [GeneratedRegex(@"^case\s+([A-Za-z_]\w*)(?:\s+(?<property>[\w.]+)\s*=(?!=|>)\s*(?<value>.+))?$", RegexOptions.None, 1000)]
    private static partial Regex CaseHeaderRegex();

    [GeneratedRegex("\"" + StringLiteral.BodyPattern + "\"|'(?:[^'\\\\]|\\\\.)*'|(?<reference>\\bcase\\.[a-z_]\\w*)", RegexOptions.None, 1000)]
    private static partial Regex CaseTokenRegex();

    [GeneratedRegex(@"^case\.([a-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex CaseReferenceRegex();

    sealed class CaseReferences : ScreenplaySyntaxWalker
    {
        internal List<CaseValueExpressionSyntax> Values { get; } = [];
        public override void VisitCaseValueExpression(CaseValueExpressionSyntax syntax) => Values.Add(syntax);
    }
}
