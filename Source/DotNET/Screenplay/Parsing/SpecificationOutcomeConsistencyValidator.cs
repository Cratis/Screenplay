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
                    var commandSlice = declarations.Slices.First(entry => ReferenceEquals(entry.Scope, resolved.Scope)).Slice;
                    ValidateEvent(specification.When!, expected, scope, resolved.Node, commandSlice, resolved.Scope, declarations, context);
                }
            }
        }
    }

    static void ValidateEvent(SpecificationCommandSyntax when, SpecificationEventSyntax expected, DeclarationScope scope, CommandSyntax command, SliceSyntax commandSlice, DeclarationScope commandScope, ConsistencyDeclarations declarations, ParserContext context)
    {
        var eventType = declarations.Event(expected.EventType, scope);
        if (eventType is null)
        {
            return;
        }

        // An ambiguous target may still be the expected event; never pretend its kind is decided.
        if (command.Produces.Any(producer => declarations.Productions.Resolve(producer.Event, commandSlice).Kind == AuthoringProductionKind.Ambiguous)) return;
        var producers = command.Produces.Where(producer => declarations.Productions.IsEventProduction(producer, commandSlice)).ToList();
        var candidates = producers.Where(producer => declarations.Event(producer.Event, commandScope) == eventType).ToList();
        if (producers.Exists(producer => declarations.Event(producer.Event, commandScope) is null))
        {
            return;
        }

        // A reachable reaction may append another occurrence of the command's own event type too. Defer
        // values to execution only when the event can follow through the closed-world occurrence graph.
        if (MayFollowCommand(when, command, commandSlice, commandScope, eventType, declarations))
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

    // Over-approximate occurrence reachability, not termination or guard truth. A reachable opaque or
    // unresolved effect defeats proof; an unrelated host signal or event reaction does not. Visited
    // declarations bound cycles without guessing whether runtime guards make them settle.
    static bool MayFollowCommand(
        SpecificationCommandSyntax when,
        CommandSyntax command,
        SliceSyntax commandSlice,
        DeclarationScope commandScope,
        EventSyntax expected,
        ConsistencyDeclarations declarations)
    {
        var pending = new Queue<EventSyntax>();
        var seen = new HashSet<EventSyntax>();
        var invoked = new HashSet<CommandSyntax>();
        foreach (var producer in command.Produces.Where(producer => declarations.Productions.IsEventProduction(producer, commandSlice) &&
            Condition(producer.When, when, command, declarations) != false))
        {
            if (declarations.Event(producer.Event, commandScope) is not { } root) return true;
            pending.Enqueue(root);
        }

        while (pending.TryDequeue(out var occurrence))
        {
            if (!seen.Add(occurrence)) continue;
            foreach (var (slice, scope) in declarations.Slices)
            {
                foreach (var reaction in slice.Reactions)
                {
                    foreach (var trigger in reaction.Triggers.Where(trigger => trigger.Source is NamedTriggerSourceSyntax named &&
                        declarations.Event(named.Name, scope) == occurrence))
                    {
                        if (trigger.File is not null || trigger.Code is not null) return true;
                        foreach (var producer in ReactionProductions.In(trigger))
                        {
                            if (declarations.Event(producer.Event, scope) is not { } produced || produced == expected) return true;
                            pending.Enqueue(produced);
                        }

                        foreach (var invocation in trigger.Invokes ?? [])
                        {
                            var resolved = declarations.Resolve(invocation.Command, scope, entry => entry.Commands, node => node.Name);
                            if (resolved is not { } target || target.Node.Handler is not null) return true;
                            if (!invoked.Add(target.Node)) continue;
                            var owner = declarations.Slices.First(entry => ReferenceEquals(entry.Scope, target.Scope)).Slice;
                            foreach (var producer in target.Node.Produces)
                            {
                                var kind = declarations.Productions.Resolve(producer.Event, owner).Kind;
                                if (kind == AuthoringProductionKind.Ambiguous) return true;
                                if (!declarations.Productions.IsEventProduction(producer, owner)) continue;
                                if (declarations.Event(producer.Event, target.Scope) is not { } produced || produced == expected) return true;
                                pending.Enqueue(produced);
                            }
                        }
                    }
                }
            }
        }

        return false;
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

            if (ProducedValue(mappings[0].Source, target.Type, when, command, declarations, out var actualValue) && !Equal(expectedValue, actualValue))
            {
                return true;
            }
        }

        return false;
    }

    static bool Equal(object? left, object? right) => left is ExactNumber || right is ExactNumber ? ExactMathFacts.Equal(left, right) : Equals(left, right);

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
            ComparisonOperator.Equal => Equal(actual, expected),
            ComparisonOperator.NotEqual => !Equal(actual, expected),
            ComparisonOperator.GreaterThan when actual is ExactNumber left && expected is ExactNumber right => ExactMathFacts.Compare(left, right) > 0,
            ComparisonOperator.GreaterThanOrEqual when actual is ExactNumber left && expected is ExactNumber right => ExactMathFacts.Compare(left, right) >= 0,
            ComparisonOperator.LessThan when actual is ExactNumber left && expected is ExactNumber right => ExactMathFacts.Compare(left, right) < 0,
            ComparisonOperator.LessThanOrEqual when actual is ExactNumber left && expected is ExactNumber right => ExactMathFacts.Compare(left, right) <= 0,
            _ => null
        };
    }
}
