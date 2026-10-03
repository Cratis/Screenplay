// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Indexes known syntax value shapes once for generated fixtures and return assertions.
/// </summary>
internal sealed partial class ResponseValueTypes
{
    readonly Dictionary<string, ConceptSyntax> _concepts;
    readonly Dictionary<string, Dictionary<string, PropertySyntax>> _types;
    readonly HashSet<string> _known;

    internal ResponseValueTypes(ApplicationSyntax application)
    {
        _concepts = application.Concepts.GroupBy(concept => concept.Name, StringComparer.Ordinal)
            .Where(group => group.Count() == 1).ToDictionary(group => group.Key, group => group.Single(), StringComparer.Ordinal);
        _types = (application.Types ?? []).GroupBy(type => type.Name, StringComparer.Ordinal).Where(group => group.Count() == 1)
            .ToDictionary(
                group => group.Key,
                group => group.Single().Properties.GroupBy(property => property.Name, StringComparer.Ordinal)
                    .Where(properties => properties.Count() == 1).ToDictionary(properties => properties.Key, properties => properties.Single(), StringComparer.Ordinal),
                StringComparer.Ordinal);
        _known = ConceptSyntax.PrimitiveTypes.Concat(_concepts.Keys).Concat(_types.Keys).ToHashSet(StringComparer.Ordinal);
    }

    internal bool CompatibleGeneratedIdentifier(ExpressionSyntax value, TypeRefSyntax type)
    {
        if (!_known.Contains(type.Name)) return true; // Unavailable imported shapes remain unresolved.

        return !type.IsOptional && !type.IsCollection && _concepts.GetValueOrDefault(type.Name)?.Type == "Uuid" && value is LiteralExpressionSyntax { Value: string uuid } && UuidValueRegex().IsMatch(uuid);
    }

    internal bool Compatible(ExpressionSyntax value, TypeRefSyntax type)
    {
        if (value is LiteralExpressionSyntax { Value: null }) return type.IsOptional;
        if (type.IsCollection) return value is ListExpressionSyntax list && list.Items.All(item => Compatible(item, type with { IsCollection = false, IsOptional = false }));
        if (_types.TryGetValue(type.Name, out var properties))
        {
            return value is ObjectExpressionSyntax obj && obj.Members.All(member => properties.TryGetValue(member.Name, out var property) && Compatible(member.Value, property.Type));
        }

        var concept = _concepts.GetValueOrDefault(type.Name);
        var primitive = concept?.Type ?? type.Name;
        if (concept?.Type == "Enum") return value is LiteralExpressionSyntax { Value: string text } && concept.Values.Contains(text, StringComparer.Ordinal);
        if (value is not LiteralExpressionSyntax literal) return !_known.Contains(type.Name);

        return primitive switch
        {
            "Uuid" => literal.Value is string uuid && UuidValueRegex().IsMatch(uuid),
            "String" => literal.Value is string,
            "Bool" => literal.Value is bool,
            "Int" => literal.Value is double integer && double.IsFinite(integer) && Math.Truncate(integer) == integer,
            "Decimal" => literal.Value is double number && double.IsFinite(number),
            "Date" => literal.Value is string date && ResponseDateValues.Compatible(date, false),
            "DateTime" => literal.Value is string instant && ResponseDateValues.Compatible(instant, true),
            _ => !_known.Contains(type.Name) // Imported shapes stay unknown; never infer their type from the value.
        };
    }

    // Shared with ResponseValidator.ts: 32 hex digits (N), hyphenated UUIDs (D), or D wrapped in
    // braces (B) / parentheses (P). Hex is case-insensitive; whitespace and X initializer notation
    // are not admitted in response/fixture values. The lookahead requires the true end, even after a newline.
    [GeneratedRegex(@"^(?:[0-9a-f]{32}|[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}|\{[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}\}|\([0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}\))(?![\s\S])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 1000)]
    private static partial Regex UuidValueRegex();
}
