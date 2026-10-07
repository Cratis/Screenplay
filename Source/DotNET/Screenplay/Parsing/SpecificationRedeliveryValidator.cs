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
                }

                SpecificationValueConsistencyValidator.ValidateValues(action.Values, @event?.Properties, declarations, context);
                var matches = specification.Given.Count(given => @event is not null && ReferenceEquals(declarations.Event(given.EventType, scope), @event) &&
                    (action.For is null || (given.For is not null && SameValue(action.For, given.For, null, declarations))) &&
                    action.Values.All(locator => given.Values.Where(value => value.Property == locator.Property).ToArray() is [var value] &&
                        SameValue(locator.Source, value.Source, declarations.Property(@event.Properties, locator.Property, out _)?.Type, declarations)));
                if (matches != 1)
                {
                    context.Error(DiagnosticCodes.UnmatchedRedeliveredOccurrence, $"Redelivery of '{action.EventType}' matches {matches} given occurrences; use 'for' or values to identify exactly one.", action.Location);
                }
            }
        }
    }

    static bool SameValue(ExpressionSyntax left, ExpressionSyntax right, TypeRefSyntax? type, ConsistencyDeclarations declarations)
    {
        if (!SpecificationValueConsistencyValidator.TryValue(left, type, declarations, out var first) ||
            !SpecificationValueConsistencyValidator.TryValue(right, type, declarations, out var second))
        {
            return false;
        }

        return first is ExactNumber || second is ExactNumber ? ExactMathFacts.Equal(first, second) : Equals(first, second);
    }
}
