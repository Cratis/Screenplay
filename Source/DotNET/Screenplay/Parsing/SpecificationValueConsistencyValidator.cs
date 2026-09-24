// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Checks enum values using each specification step's own declaration, not a global field-name map.
/// </summary>
internal static class SpecificationValueConsistencyValidator
{
    /// <summary>
    /// Validates enum assignments in command, event, read-model and query steps.
    /// </summary>
    /// <param name="declarations">The application declarations.</param>
    /// <param name="context">The diagnostic sink.</param>
    public static void Validate(ConsistencyDeclarations declarations, ParserContext context)
    {
        foreach (var (slice, scope) in declarations.Slices)
        {
            foreach (var specification in slice.Specifications)
            {
                if (specification.When is { } when)
                {
                    var command = declarations.Resolve(when.CommandType, scope, item => item.Commands, node => node.Name);
                    ValidateValues(when.Values, command?.Node.Properties, declarations, context);
                }

                foreach (var step in specification.Given.Concat(specification.ThenEvents))
                {
                    ValidateValues(step.Values, declarations.Event(step.EventType, scope)?.Properties, declarations, context);
                }

                foreach (var step in (specification.GivenReadModels ?? []).Concat(specification.ThenReadModels ?? []))
                {
                    ValidateValues(step.Properties, declarations.ViewProperties(step.Name, scope), declarations, context);
                }

                ValidateQueries(specification, scope, declarations, context);
            }
        }
    }

    /// <summary>
    /// Recognizes a statically stated scalar value, including enum members.
    /// </summary>
    /// <param name="expression">The authored expression.</param>
    /// <param name="type">The field's own type.</param>
    /// <param name="declarations">The application declarations.</param>
    /// <param name="value">The recognized scalar.</param>
    /// <returns>Whether the value is statically known.</returns>
    public static bool TryValue(ExpressionSyntax expression, TypeRefSyntax? type, ConsistencyDeclarations declarations, out object? value)
    {
        value = null;
        var enumeration = type is null ? null : declarations.Enumeration(type);
        if (type?.IsCollection == true && expression is ListExpressionSyntax list &&
            declarations.Compatible(type with { IsCollection = false }, type with { IsCollection = false }) == true)
        {
            var items = new List<string>();
            foreach (var item in list.Items)
            {
                if (!TryValue(item, type with { IsCollection = false, IsOptional = false }, declarations, out var known))
                {
                    return false;
                }

                items.Add(Canonical(known));
            }

            value = new StructuredKnownValue($"[{string.Join(',', items)}]");
            return true;
        }

        if (type?.IsCollection == false && expression is ObjectExpressionSyntax obj && declarations.TypeProperties(type.Name) is { } properties)
        {
            var members = new List<(string Name, string Value)>();
            foreach (var member in obj.Members)
            {
                var property = declarations.Property(properties, member.Name, out _);
                if (property is null || !TryValue(member.Value, property.Type, declarations, out var known))
                {
                    return false;
                }

                members.Add((member.Name, Canonical(known)));
            }

            value = new StructuredKnownValue($"{{{string.Join(',', members.OrderBy(member => member.Name, StringComparer.Ordinal).Select(member => $"{JsonSerializer.Serialize(member.Name)}:{member.Value}"))}}}");
            return true;
        }

        if (expression is LiteralExpressionSyntax literal)
        {
            value = enumeration is not null && literal.Value is string text ? Member(text, enumeration.Name) : literal.Value;
            return true;
        }

        if (expression is PathExpressionSyntax path && enumeration is not null)
        {
            value = Member(path.Path, enumeration.Name);
            return true;
        }

        return false;
    }

    internal static void ValidateStructuredMappings(IEnumerable<PropertyMappingSyntax> assignments, IEnumerable<PropertySyntax>? properties, ConsistencyDeclarations declarations, ParserContext context)
    {
        ValidateValues(assignments.Where(assignment => assignment.Source is ObjectExpressionSyntax or ListExpressionSyntax), properties, declarations, context);
    }

    static string Canonical(object? value) => value is StructuredKnownValue structured ? structured.Canonical : JsonSerializer.Serialize(value);

    static void ValidateQueries(SpecificationSyntax specification, DeclarationScope scope, ConsistencyDeclarations declarations, ParserContext context)
    {
        foreach (var step in specification.ThenQueries)
        {
            var query = declarations.Resolve(step.Query, scope, item => item.Queries, node => node.Name);
            if (query is not { } resolved)
            {
                continue;
            }

            var parameters = (resolved.Node.By is null ? Enumerable.Empty<QueryParameterSyntax>() : [resolved.Node.By]).Concat(resolved.Node.Filters);
            ValidateValues(step.Arguments, parameters.Select(parameter => new PropertySyntax(parameter.Name, parameter.Type, parameter.Location)), declarations, context);
            foreach (var result in step.Results)
            {
                ValidateValues(result.Properties, declarations.ViewProperties(resolved.Node.ReturnType.Name, resolved.Scope), declarations, context);
            }
        }
    }

    static void ValidateValues(IEnumerable<PropertyMappingSyntax> assignments, IEnumerable<PropertySyntax>? properties, ConsistencyDeclarations declarations, ParserContext context)
    {
        foreach (var assignment in assignments)
        {
            var property = declarations.Property(properties, assignment.Property, out _);
            if (property is not null)
            {
                ValidateValue(assignment.Source, property.Type, assignment.Property, declarations, context);
            }
        }
    }

    static void ValidateValue(ExpressionSyntax expression, TypeRefSyntax type, string path, ConsistencyDeclarations declarations, ParserContext context)
    {
        var properties = declarations.TypeProperties(type.Name);
        var enumeration = declarations.Enumeration(type);
        if (expression is LiteralExpressionSyntax { Value: null } && type.IsOptional)
        {
            return;
        }

        if (expression is ListExpressionSyntax list)
        {
            if (!type.IsCollection && (properties is not null || enumeration is not null || ConceptSyntax.PrimitiveTypes.Contains(type.Name)))
            {
                ShapeError(expression, path, "a scalar or object", context);
                return;
            }

            if (type.IsCollection)
            {
                foreach (var item in list.Items)
                {
                    ValidateValue(item, type with { IsCollection = false, IsOptional = false }, path, declarations, context);
                }
            }

            return;
        }

        if (expression is ObjectExpressionSyntax obj)
        {
            if (type.IsCollection || (properties is null && (enumeration is not null || ConceptSyntax.PrimitiveTypes.Contains(type.Name))))
            {
                ShapeError(expression, path, type.IsCollection ? "a list" : "a scalar", context);
                return;
            }

            if (properties is not null)
            {
                foreach (var item in obj.Members)
                {
                    var property = declarations.Property(properties, item.Name, out var missing);
                    if (property is null && missing)
                    {
                        context.Error(DiagnosticCodes.UnknownStructuredValueMember, $"Unknown property '{item.Name}' in structured value for '{type.Name}'", item.Location);
                    }
                    else if (property is not null)
                    {
                        ValidateValue(item.Value, property.Type, $"{path}.{item.Name}", declarations, context);
                    }
                }
            }

            return;
        }

        if (type.IsCollection && expression is LiteralExpressionSyntax)
        {
            ShapeError(expression, path, "a list", context);
            return;
        }

        if (properties is not null && expression is LiteralExpressionSyntax)
        {
            ShapeError(expression, path, "an object", context);
            return;
        }

        if (enumeration is not null && TryValue(expression, type, declarations, out var value) &&
            !(value is null && type.IsOptional) && (value is not string member || !enumeration.Values.Contains(member, StringComparer.Ordinal)))
        {
            context.Error(
                DiagnosticCodes.UnknownSpecificationEnumMember,
                $"Value '{value ?? "null"}' for '{path}' is not a declared member of enum '{enumeration.Name}'",
                expression.Location);
        }
    }

    static void ShapeError(ExpressionSyntax expression, string path, string expected, ParserContext context) =>
        context.Error(DiagnosticCodes.IncompatibleStructuredValue, $"Value for '{path}' must be {expected}", expression.Location);

    static string Member(string value, string enumeration) => value.StartsWith($"{enumeration}.", StringComparison.Ordinal)
        ? value[(enumeration.Length + 1)..]
        : value;

    sealed record StructuredKnownValue(string Canonical);
}
