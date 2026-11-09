// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>Indexes operations with the authorization inherited from their containing scopes.</summary>
internal static class AgentOperationCatalog
{
    public static IReadOnlyList<Target> Of(ApplicationSyntax application) =>
        [.. application.Modules.SelectMany(module => InFeatures(module.Features, [module.Name], module.Authorize is not null))];

    static IEnumerable<Target> InFeatures(IEnumerable<FeatureSyntax> features, IReadOnlyList<string> parents, bool authorized)
    {
        foreach (var feature in features)
        {
            var path = parents.Append(feature.Name).ToArray();
            var gated = authorized || feature.Authorize is not null;
            foreach (var slice in feature.Slices)
            {
                var scope = new DeclarationScope([.. path, slice.Name]);
                foreach (var target in InSlice(slice, scope, gated)) yield return target;
            }

            foreach (var target in InFeatures(feature.Features, path, gated))
            {
                yield return target;
            }
        }
    }

    static IEnumerable<Target> InSlice(SliceSyntax slice, DeclarationScope scope, bool gated)
    {
        foreach (var command in slice.Commands)
        {
            yield return new(AgentOperationKind.Command, new(command.Name, scope), gated || command.Authorize is not null);
        }

        foreach (var query in slice.Queries)
        {
            yield return new(AgentOperationKind.Query, new(query.Name, scope), gated || query.Authorize is not null)
            {
                IsGlobal = query.Scope == QuerySyntax.GlobalScope,
                RefusedQueryKind = query.Performer is null ? null : "performed query"
            };
        }

        foreach (var query in slice.GraphQueries)
        {
            yield return new(AgentOperationKind.Query, new(query.Name, scope), gated || query.Authorize is not null) { RefusedQueryKind = "graph query" };
        }
    }

    internal sealed record Target(AgentOperationKind Kind, Declaration Declaration, bool Authorized)
    {
        public string QualifiedName => string.Join('.', Declaration.Scope.Segments.Append(Declaration.Name));

        public bool IsGlobal { get; init; }

        public string? RefusedQueryKind { get; init; }
    }
}
