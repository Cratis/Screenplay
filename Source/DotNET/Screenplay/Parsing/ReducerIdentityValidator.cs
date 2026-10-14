// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Warns about statically inconsistent nominal reducer identities, not runtime identifier values.
/// </summary>
internal static class ReducerIdentityValidator
{
    internal static void Validate(ApplicationSyntax application, ConsistencyDeclarations declarations, ParserContext context)
    {
        if (!declarations.Slices.Any(entry => (entry.Slice.Reducers ?? []).Any())) return;
        var domains = new AuthoringIdentityDomains(application, declarations);
        var producers = AuthoringProducers.In(application, declarations).ToLookup<AuthoringProducer, EventSyntax>(producer => producer.Event, ReferenceEqualityComparer.Instance);
        var catalog = new EventSourceCatalog(application);
        foreach (var (slice, scope) in declarations.Slices)
        {
            foreach (var reducer in slice.Reducers ?? [])
            {
                if (declarations.Resolve(reducer.ReadModel, scope, owner => owner.ReadModels ?? [], model => model.Name) is not { } target ||
                    declarations.Event(reducer.ReadModel, scope) is not null || ProjectionBuilds(target.Node, declarations))
                {
                    continue;
                }
                if (declarations.ViewKey(reducer.ReadModel, scope).Length > 1) continue;
                if (reducer.From is { } filter && !ValidFilter(filter, catalog)) continue;
                var evidence = reducer.Rules.Select(rule => Evidence(rule, scope, reducer.From, declarations, domains, producers, catalog)).ToArray();
                var known = evidence.SelectMany(item => item.Domains.OfType<AuthoringIdentityDomain>()).Distinct().ToArray();
                if (evidence.Length > 0 && evidence.All(item => item.Complete) && known.Length > 1)
                {
                    var pairs = evidence.SelectMany(item => item.Domains.OfType<AuthoringIdentityDomain>().Select(domain => $"{item.Event}/{domain.Name}"))
                        .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);
                    context.Warning(
                        DiagnosticCodes.ReducerIdentityMismatch,
                        $"Reducer '{reducer.Name}' observes events with different event-source identity types: {string.Join(", ", pairs)}. They do not establish one shared instance identity. Use a projection with explicit property keys, or joins for related existing instances.",
                        reducer.Location);
                    continue;
                }
                foreach (var (querySlice, queryScope) in declarations.Slices)
                {
                    foreach (var query in querySlice.Queries.Where(query => !query.ReturnType.IsCollection && query.Performer is null && query.By is not null && !query.ByParts.Any()))
                    {
                        if (declarations.Resolve(query.ReturnType.Name, queryScope, owner => owner.ReadModels ?? [], model => model.Name) is not { } queried ||
                            !ReferenceEquals(queried.Node, target.Node) || !ValidLookup(query, target.Node, queried.Scope, queryScope, declarations, domains))
                        {
                            continue;
                        }
                        var queryDomain = domains.Resolve(query.By!.Type, queryScope)!;
                        var incompatible = evidence.FirstOrDefault(item => item.Complete && item.Domains.Length > 0 && item.Domains.All(domain => domain != queryDomain));
                        if (incompatible is null) continue;
                        var names = incompatible.Domains.OfType<AuthoringIdentityDomain>().Select(domain => domain.Name).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);
                        context.Warning(
                            DiagnosticCodes.ReducerIdentityMismatch,
                            $"Query '{query.Name}' looks up '{target.Node.Name}' by '{query.By!.Name}' ({queryDomain.Name}), but reducer '{reducer.Name}' observes '{incompatible.Event}' with event-source identity {string.Join(", ", names)}. Reducer instances are keyed by each event's event-source id. Use a projection with a property key on each relevant 'from', or a join to update existing instances.",
                            query.Location);
                    }
                }
            }
        }
    }

    static bool ProjectionBuilds(ReadModelSyntax target, ConsistencyDeclarations declarations) => declarations.Slices.Any(entry => entry.Slice.Projections.Any(projection =>
        declarations.Resolve(projection.ReadModel ?? projection.Name, entry.Scope, owner => owner.ReadModels ?? [], model => model.Name) is { } resolved && ReferenceEquals(resolved.Node, target)));

    static bool ValidLookup(QuerySyntax query, ReadModelSyntax model, DeclarationScope modelScope, DeclarationScope queryScope, ConsistencyDeclarations declarations, AuthoringIdentityDomains domains)
    {
        var by = query.By!;
        var key = declarations.ViewKey(query.ReturnType.Name, queryScope);
        var properties = model.Properties.Where(property => property.Name == by.Name).ToArray();
        var domain = domains.Resolve(by.Type, queryScope);

        return domain is not null && domain.Kind != AuthoringIdentityDomainKind.Composite && key.Length == 1 && key[0].Name == by.Name && properties is [var property] &&
            domains.Resolve(property.Type, modelScope) == domain;
    }

    static EventEvidence Evidence(ReducerRuleSyntax rule, DeclarationScope scope, ObserverFilterSyntax? filter, ConsistencyDeclarations declarations, AuthoringIdentityDomains domains, ILookup<EventSyntax, AuthoringProducer> producers, EventSourceCatalog catalog)
    {
        if (declarations.Event(rule.Event, scope) is not { } @event) return new(rule.Event, [null]);
        var all = producers[@event].ToArray();
        if (all.Length == 0) return new(rule.Event, [null]);
        var feasible = all.Where(producer => Feasible(producer, filter, catalog));

        return new(rule.Event, [.. feasible.Select(producer => (filter is null || producer.RouteKnown) && ValidRoute(producer.Route, catalog) ? domains.Resolve(producer.IdentityType, producer.Scope) : null)]);
    }

    static bool ValidFilter(ObserverFilterSyntax filter, EventSourceCatalog catalog) =>
        (filter.Stream is null ? catalog.Resolve(filter.EventSource) : catalog.Resolve(filter.EventSource, filter.Stream)).Kind == EventSourceResolutionKind.Unique;

    static bool ValidRoute(CommandStreamSyntax? route, EventSourceCatalog catalog) => route is null ||
        (route.PropertyCandidate is null && catalog.Resolve(route.EventSource, route.Stream).Kind == EventSourceResolutionKind.Unique);

    static bool Feasible(AuthoringProducer producer, ObserverFilterSyntax? filter, EventSourceCatalog catalog)
    {
        if (filter is null || !producer.RouteKnown || !ValidRoute(producer.Route, catalog)) return true;

        return producer.Route is { } route && route.EventSource == filter.EventSource && (filter.Stream is null || route.Stream == filter.Stream);
    }

    sealed record EventEvidence(string Event, AuthoringIdentityDomain?[] Domains)
    {
        internal bool Complete => Domains.All(domain => domain is not null);
    }
}
