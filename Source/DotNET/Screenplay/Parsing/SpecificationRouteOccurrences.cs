// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Parsing;

internal sealed record SpecificationRouteOccurrence(
    SpecificationEventSyntax Node, EventSyntax? Event, CommandSyntax? Command,
    bool Required, bool Expected, bool ValidateRoute, bool ValidateFor,
    EffectiveSpecificationStep? Step)
{
    internal static IEnumerable<SpecificationRouteOccurrence> In(ConsistencyDeclarations declarations, EffectiveSpecificationApplication expansion)
    {
        foreach (var resolved in expansion.ResolvedExamples.Where(resolved => resolved.Kind == "event"))
        {
            var example = resolved.Example;
            yield return new(new(example.Type, example.Values, example.Location) { For = example.For, Stream = example.Stream, NoStream = example.NoStream },
                (EventSyntax)resolved.Type, null, false, false, true, true, null);
        }
        foreach (var (slice, scope) in declarations.Slices)
        {
            foreach (var specification in slice.Specifications)
            {
                var command = specification.When is { } action
                    ? declarations.Resolve(action.CommandType, scope, owner => owner.Commands, item => item.Name)?.Node : null;
                var steps = expansion.Specifications.FirstOrDefault(pair => ReferenceEquals(pair.Effective, specification))?.Steps ?? [];
                foreach (var (node, required, expected) in specification.Given.Select(node => (node, true, false))
                    .Concat(specification.WhenAppended is { } appended ? [(appended, true, false)] : [])
                    .Concat(specification.ThenEvents.Select(node => (node, false, true))))
                {
                    var step = steps.FirstOrDefault(step => ReferenceEquals(step.Effective, node));
                    var inherited = step?.Route?.Origin == SpecificationValueOrigin.Example;
                    var unchanged = inherited && ReferenceEquals(node.For, step!.Example!.For);
                    yield return new(node, declarations.Event(node.EventType, scope), command, required, expected, !inherited, !unchanged, step);
                }
                if (specification.WhenRedelivered is { } locator)
                {
                    yield return new(new(locator.EventType, locator.Values, locator.Location) { For = locator.For, Stream = locator.Stream, NoStream = locator.NoStream },
                        declarations.Event(locator.EventType, scope), null, false, false, true, true, null);
                }
            }
        }
    }

    internal void ContextualError(ParserContext context, string code, string message, SourceLocation location)
    {
        if (Step?.Example is { } example)
        {
            context.Error(code, $"{message} Example '{example.Name}' is used here.", Step.Authored.Location);
        }
        else
        {
            context.Error(code, message, location);
        }
    }
}
