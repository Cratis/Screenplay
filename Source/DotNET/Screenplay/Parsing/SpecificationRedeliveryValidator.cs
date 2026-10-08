// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Checks that a redelivery action names one observer and exactly one prior event occurrence.
/// </summary>
internal static class SpecificationRedeliveryValidator
{
    internal static void Validate(ConsistencyDeclarations declarations, ParserContext context)
    {
        foreach (var (slice, scope) in declarations.Slices)
        {
            foreach (var specification in slice.Specifications.Where(specification => specification.WhenRedelivered is not null))
            {
                var action = specification.WhenRedelivered!;
                var @event = declarations.Event(action.EventType, scope);
                var reaction = declarations.Resolve(action.Reaction, scope, owner => owner.Reactions, node => node.Name);
                if (reaction is not { } resolved || @event is null || !resolved.Node.Triggers.Any(trigger =>
                    trigger.Source is NamedTriggerSourceSyntax named && ReferenceEquals(declarations.Event(named.Name, resolved.Scope), @event)))
                {
                    context.Error(DiagnosticCodes.UnknownRedeliveryReaction, $"Reaction '{action.Reaction}' must resolve unambiguously and observe event '{action.EventType}'.", action.Location);
                    continue;
                }

                SpecificationValueConsistencyValidator.ValidateValues(action.Values, @event.Properties, declarations, context);
                var candidates = specification.Given.Where(given => ReferenceEquals(declarations.Event(given.EventType, scope), @event))
                    .Select(given => Matches(given, action, @event, declarations)).ToArray();
                var matches = candidates.Count(match => match == true);
                if (matches < 2 && candidates.Any(match => match is null))
                {
                    context.Error(DiagnosticCodes.UnmatchedRedeliveredOccurrence, $"Cannot locate redelivery of '{action.EventType}' uniquely: a given source or locator value is not decidable; state explicit concrete 'for' and values.", action.Location);
                }
                else if (matches != 1)
                {
                    context.Error(DiagnosticCodes.UnmatchedRedeliveredOccurrence, $"Redelivery of '{action.EventType}' matches {matches} given occurrences; use 'for' or values to identify exactly one.", action.Location);
                }
            }
        }
    }

    static bool? Matches(SpecificationEventSyntax given, SpecificationRedeliverySyntax action, EventSyntax @event, ConsistencyDeclarations declarations)
    {
        // Binding retains a null source for a given without 'for'; the producer supplies its type,
        // not a concrete identity. Do not invent a destination or count an undecidable locator as zero.
        var comparisons = new List<bool?>();
        if (action.For is not null) comparisons.Add(given.For is null ? null : SameValue(action.For, given.For, null, declarations));
        foreach (var locator in action.Values)
        {
            comparisons.Add(given.Values.Where(value => value.Property == locator.Property).ToArray() is [var value]
                ? SameValue(locator.Source, value.Source, declarations.Property(@event.Properties, locator.Property, out _)?.Type, declarations)
                : null);
        }

        if (comparisons.Contains(false)) return false;

        return comparisons.Contains(null) ? null : true;
    }

    static bool? SameValue(ExpressionSyntax left, ExpressionSyntax right, TypeRefSyntax? type, ConsistencyDeclarations declarations)
    {
        if (!SpecificationValueConsistencyValidator.TryValue(left, type, declarations, out var first) ||
            !SpecificationValueConsistencyValidator.TryValue(right, type, declarations, out var second))
        {
            return null;
        }

        return first is ExactNumber || second is ExactNumber ? ExactMathFacts.Equal(first, second) : Equals(first, second);
    }
}
