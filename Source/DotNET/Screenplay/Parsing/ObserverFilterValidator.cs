// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

internal static class ObserverFilterValidator
{
    internal static void Validate(ApplicationSyntax application, ConsistencyDeclarations declarations, ParserContext context)
    {
        foreach (var (slice, scope) in declarations.Slices)
        {
            foreach (var reaction in slice.Reactions.Where(reaction => reaction.From is not null))
            {
                var filter = reaction.From!;
                if (!Resolve(filter, application, context)) continue;
                foreach (var trigger in reaction.Triggers)
                {
                    if (trigger.Source is not NamedTriggerSourceSyntax named || declarations.Event(named.Name, scope) is not { } @event)
                    {
                        context.Error(DiagnosticCodes.InvalidObserverFilter, "A filtered reaction requires only 'when <Event>' triggers on declared events.", trigger.Location);
                        continue;
                    }
                    WarnIfExcluded(filter, @event, application, declarations, context);
                }
            }
            foreach (var reducer in (slice.Reducers ?? []).Where(reducer => reducer.From is not null))
            {
                var filter = reducer.From!;
                if (!Resolve(filter, application, context)) continue;
                foreach (var rule in reducer.Rules)
                {
                    if (declarations.Event(rule.Event, scope) is { } @event) WarnIfExcluded(filter, @event, application, declarations, context);
                }
            }
        }
    }

    static bool Resolve(ObserverFilterSyntax filter, ApplicationSyntax application, ParserContext context)
    {
        var sources = application.EventSources.Where(source => source.Name == filter.EventSource).ToArray();
        if (sources.Length == 1 && (filter.Stream is null || sources[0].Streams.Count(stream => stream.Name == filter.Stream) == 1)) return true;
        context.Error(DiagnosticCodes.InvalidObserverFilter, "An observer filter must name one physical event source and, when supplied, one stream belonging to it.", filter.Location);

        return false;
    }

    static void WarnIfExcluded(ObserverFilterSyntax filter, EventSyntax @event, ApplicationSyntax application, ConsistencyDeclarations declarations, ParserContext context)
    {
        if (@event.Origin is not null || declarations.Slices.Any(entry => entry.Slice.Commands.Any(command => command.Handler is not null))) return;
        var routes = new List<CommandStreamSyntax?>();
        if (@event.Visibility == EventVisibility.Public && declarations.Slices.Any(entry =>
            entry.Slice.Projections.Any(projection => ReferenceEquals(declarations.Event(projection.ReadModel ?? projection.Name, entry.Scope), @event)) ||
            (entry.Slice.Reducers ?? []).Any(reducer => ReferenceEquals(declarations.Event(reducer.ReadModel, entry.Scope), @event))))
        {
            routes.Add(null);
        }
        foreach (var (slice, scope) in declarations.Slices)
        {
            foreach (var command in slice.Commands)
            {
                routes.AddRange(command.Produces.Where(produced => ReferenceEquals(declarations.Event(produced.Event, scope), @event)).Select(produced => produced.Stream ?? command.Stream));
            }
            foreach (var produced in slice.Reactions.SelectMany(reaction => reaction.Triggers).SelectMany(trigger => (trigger.Produces ?? []).Concat((trigger.Invokes ?? []).SelectMany(invoked => invoked.OnRefused).SelectMany(refusal => refusal.Produces))))
            {
                if (ReferenceEquals(declarations.Event(produced.Event, scope), @event)) routes.Add(null);
            }
            foreach (var append in slice.Captures.SelectMany(capture => capture.Appends.Concat(capture.Children.SelectMany(child => child.Appends)).Concat(capture.Nested.SelectMany(nested => nested.Appends))))
            {
                if (ReferenceEquals(declarations.Event(append.Event, scope), @event)) routes.Add(null);
            }
        }
        if (routes.Count > 0 && routes.TrueForAll(route => route is null || route.EventSource != filter.EventSource || (filter.Stream is not null && route.Stream != filter.Stream)))
        {
            context.Warning(DiagnosticCodes.ObserverFilterExcludesEveryProducer, $"The observer filter excludes every statically known producer of '{@event.Name}'.", filter.Location);
        }
    }
}
