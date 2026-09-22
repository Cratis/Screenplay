// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            var enumeration = property is null ? null : declarations.Enumeration(property.Type);
            if (enumeration is null || !TryValue(assignment.Source, property!.Type, declarations, out var value))
            {
                continue;
            }

            if (value is null && property!.Type.IsOptional)
            {
                continue;
            }

            if (value is not string member || !enumeration.Values.Contains(member, StringComparer.Ordinal))
            {
                context.Error(
                    DiagnosticCodes.UnknownSpecificationEnumMember,
                    $"Value '{value ?? "null"}' for '{assignment.Property}' is not a declared member of enum '{enumeration.Name}'",
                    assignment.Location);
            }
        }
    }

    static string Member(string value, string enumeration) => value.StartsWith($"{enumeration}.", StringComparison.Ordinal)
        ? value[(enumeration.Length + 1)..]
        : value;
}
