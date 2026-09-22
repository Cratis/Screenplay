// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Finds contradictions between stated specification values and declarative producer mappings.
/// </summary>
internal static class SpecificationOutcomeConsistencyValidator
{
    /// <summary>
    /// Checks outcomes without executing handlers or interpreting opaque expressions.
    /// </summary>
    /// <param name="declarations">The application declarations.</param>
    /// <param name="context">The diagnostic sink.</param>
    public static void Validate(ConsistencyDeclarations declarations, ParserContext context)
    {
        foreach (var (slice, scope) in declarations.Slices)
        {
            foreach (var specification in slice.Specifications.Where(specification => specification.When is not null))
            {
                var command = declarations.Resolve(specification.When!.CommandType, scope, item => item.Commands, node => node.Name);
                if (command is not { } resolved || resolved.Node.Handler is not null || !resolved.Node.Produces.Any())
                {
                    continue;
                }

                foreach (var expected in specification.ThenEvents)
                {
                    ValidateEvent(specification.When!, expected, scope, resolved.Node, resolved.Scope, declarations, context);
                }
            }
        }
    }

    static void ValidateEvent(SpecificationCommandSyntax when, SpecificationEventSyntax expected, DeclarationScope scope, CommandSyntax command, DeclarationScope commandScope, ConsistencyDeclarations declarations, ParserContext context)
    {
        var eventType = declarations.Event(expected.EventType, scope);
        if (eventType is null)
        {
            return;
        }

        var producers = command.Produces.ToList();
        var candidates = producers.Where(producer => declarations.Event(producer.Event, commandScope) == eventType).ToList();
        if (producers.Exists(producer => declarations.Event(producer.Event, commandScope) is null))
        {
            return;
        }

        if (candidates.TrueForAll(producer => Contradicts(producer, when, expected, command, eventType, declarations)))
        {
            context.Error(
                DiagnosticCodes.UnreachableSpecificationOutcome,
                $"Specification outcome '{expected.EventType}' cannot be produced by '{command.Name}' from the stated 'when' values under its declared mappings",
                expected.Location);
        }
    }

    static bool Contradicts(ProducesSyntax producer, SpecificationCommandSyntax when, SpecificationEventSyntax expected, CommandSyntax command, EventSyntax eventType, ConsistencyDeclarations declarations)
    {
        if (Condition(producer.When, when, command, declarations) == false)
        {
            return true;
        }

        foreach (var assignment in expected.Values)
        {
            var target = declarations.Property(eventType.Properties, assignment.Property, out _);
            var mappings = producer.Mappings.Where(mapping => mapping.Property == assignment.Property).ToList();
            if (target is null || mappings.Count != 1 ||
                !SpecificationValueConsistencyValidator.TryValue(assignment.Source, target.Type, declarations, out var expectedValue))
            {
                continue;
            }

            if (ProducedValue(mappings[0].Source, target.Type, when, command, declarations, out var actualValue) && !Equals(expectedValue, actualValue))
            {
                return true;
            }
        }

        return false;
    }

    static bool ProducedValue(ExpressionSyntax expression, TypeRefSyntax target, SpecificationCommandSyntax when, CommandSyntax command, ConsistencyDeclarations declarations, out object? value)
    {
        if (expression is PathExpressionSyntax path)
        {
            var property = declarations.Property(command.Properties, path.Path, out _);
            if (property is not null)
            {
                var values = when.Values.Where(assignment => assignment.Property == path.Path).ToList();
                value = null;
                return values.Count == 1 && SpecificationValueConsistencyValidator.TryValue(values[0].Source, property.Type, declarations, out value);
            }

            // A path into state, or an external expression, is not an enum literal merely because the
            // target is an enum. Only its actual declared members are decidable here.
            var enumeration = declarations.Enumeration(target);
            if (enumeration?.Values.Any(member => path.Path == member || path.Path == $"{enumeration.Name}.{member}") != true)
            {
                value = null;
                return false;
            }
        }

        return SpecificationValueConsistencyValidator.TryValue(expression, target, declarations, out value);
    }

    static bool? Condition(ConditionSyntax? condition, SpecificationCommandSyntax when, CommandSyntax command, ConsistencyDeclarations declarations)
    {
        if (condition is null)
        {
            return true;
        }

        if (condition is LogicalConditionSyntax logical)
        {
            var left = Condition(logical.Left, when, command, declarations);
            var right = Condition(logical.Right, when, command, declarations);
            return (logical.Operator, left, right) switch
            {
                (LogicalOperator.And, false, _) or (LogicalOperator.And, _, false) => false,
                (LogicalOperator.And, true, true) => true,
                (LogicalOperator.Or, true, _) or (LogicalOperator.Or, _, true) => true,
                (LogicalOperator.Or, false, false) => false,
                _ => null
            };
        }

        if (condition is not ComparisonConditionSyntax comparison)
        {
            return null;
        }

        var property = declarations.Property(command.Properties, comparison.Left, out _);
        var values = when.Values.Where(value => value.Property == comparison.Left).ToList();
        if (property is null || values.Count != 1 ||
            !SpecificationValueConsistencyValidator.TryValue(values[0].Source, property.Type, declarations, out var actual) ||
            !ProducedValue(comparison.Right, property.Type, when, command, declarations, out var expected))
        {
            return null;
        }

        return comparison.Operator switch
        {
            ComparisonOperator.Equal => Equals(actual, expected),
            ComparisonOperator.NotEqual => !Equals(actual, expected),
            _ => null
        };
    }
}
