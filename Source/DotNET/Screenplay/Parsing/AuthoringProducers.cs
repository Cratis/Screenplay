// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Discovers producers across the assembled application without binding executable code.
/// </summary>
internal static class AuthoringProducers
{
    internal static IEnumerable<AuthoringProducer> In(ApplicationSyntax application, ConsistencyDeclarations declarations)
    {
        var opaque = declarations.Slices.Any(entry => entry.Slice.Commands.Any(command => command.Handler is not null) ||
            entry.Slice.Reactions.SelectMany(reaction => reaction.Triggers).Any(trigger => trigger.Code is not null || trigger.File is not null));
        foreach (var (slice, scope) in declarations.Slices)
        {
            foreach (var @event in EventDeclarations.In(slice).Where(@event => opaque || @event.Origin is not null || @event.Visibility == EventVisibility.Public))
            {
                yield return new(@event, scope, AuthoringProducerKind.Opaque);
            }
            foreach (var command in slice.Commands)
            {
                var productions = command.Produces.Where(produced => declarations.Productions.IsEventProduction(produced, slice)).ToArray();
                foreach (var produced in productions)
                {
                    if (declarations.Event(produced.Event, scope) is not { } @event) continue;
                    yield return new(@event, scope, AuthoringProducerKind.Command, CommandDestinationTypes.DestinationType(command, produced, productions, declarations), command, produced.Stream ?? command.Stream, !command.StreamCandidates.Skip(1).Any())
                    {
                        IdentityType = ReducerDestination(command, produced, productions, declarations)
                    };
                }
            }
            foreach (var trigger in slice.Reactions.SelectMany(reaction => reaction.Triggers))
            {
                foreach (var produced in trigger.Produces ?? [])
                {
                    if (declarations.Event(produced.Event, scope) is not { } @event) continue;
                    yield return Reaction(@event, scope, trigger, produced, AuthoringProducerKind.Reaction, application, declarations);
                }
                foreach (var produced in (trigger.Invokes ?? []).SelectMany(invoked => invoked.OnRefused).SelectMany(refusal => refusal.Produces))
                {
                    if (declarations.Event(produced.Event, scope) is not { } @event) continue;
                    yield return Reaction(@event, scope, trigger, produced, AuthoringProducerKind.Refusal, application, declarations);
                }
            }
            foreach (var append in slice.Captures.SelectMany(capture => capture.Appends.Concat(capture.Children.SelectMany(child => child.Appends)).Concat(capture.Nested.SelectMany(nested => nested.Appends))))
            {
                if (declarations.Event(append.Event, scope) is { } @event) yield return new(@event, scope, AuthoringProducerKind.Capture);
            }
        }
        var root = new DeclarationScope([]);
        foreach (var seeded in (application.Seeds ?? []).SelectMany(seed => seed.Groups).SelectMany(group => group.Events))
        {
            if (declarations.Event(seeded.Event, root) is { } @event) yield return new(@event, root, AuthoringProducerKind.Opaque);
        }
    }

    static TypeRefSyntax? ReducerDestination(CommandSyntax command, ProducesSyntax produced, ProducesSyntax[] productions, ConsistencyDeclarations declarations)
    {
        if (produced.For is null && produced.InlineEvent is null)
        {
            return command.Properties.Where(property => property.IsIdentifier).ToArray() is [var identifier] ? identifier.Type : null;
        }

        return CommandDestinationTypes.DestinationType(command, produced, productions, declarations);
    }

    static AuthoringProducer Reaction(EventSyntax @event, DeclarationScope scope, ReactionTriggerSyntax trigger, ProducesSyntax produced, AuthoringProducerKind kind, ApplicationSyntax application, ConsistencyDeclarations declarations)
    {
        var type = ReactionDestination(trigger, produced.For, scope, application, declarations);

        return new(@event, scope, kind, type, RouteKnown: produced.For is not null);
    }

    static TypeRefSyntax? ReactionDestination(ReactionTriggerSyntax trigger, ExpressionSyntax? destination, DeclarationScope scope, ApplicationSyntax application, ConsistencyDeclarations declarations)
    {
        if (destination is LiteralExpressionSyntax { Value: string text } && !string.IsNullOrWhiteSpace(text)) return new("String", false, false, destination.Location);
        if (destination is not PathExpressionSyntax path) return null;
        IEnumerable<PropertySyntax>? properties = null;
        if (trigger.Source is NamedTriggerSourceSyntax named)
        {
            properties = declarations.Event(named.Name, scope)?.Properties;
            if (properties is null)
            {
                var triggers = (application.Triggers ?? []).Where(item => item.Name == named.Name).ToArray();
                if (triggers is [var declared]) properties = declared.Data.Where(datum => datum.Type is not null).Select(datum => new PropertySyntax(datum.Name, datum.Type!, datum.Location));
            }
        }

        // Typed occurrence data can supply an integration/clock value, but not override a declared event field.
        properties ??= trigger.Data.Where(datum => datum.Type is not null).Select(datum => new PropertySyntax(datum.Name, datum.Type!, datum.Location));

        var type = declarations.Property(properties, path.Path, out _)?.Type;
        var segments = path.Path.Split('.');
        for (var depth = 1; depth < segments.Length; depth++)
        {
            if (declarations.Property(properties, string.Join('.', segments.Take(depth)), out _)?.Type is { } parent && (parent.IsOptional || parent.IsCollection)) return null;
        }

        return type;
    }
}
