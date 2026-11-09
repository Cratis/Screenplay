// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

static class SemanticAgentOperationCatalog
{
    internal static IEnumerable<Target> Of(SemanticApplication application) =>
        application.Modules.SelectMany(module => module.Features.SelectMany(feature => Features(feature, [module.Name])));

    static IEnumerable<Target> Features(SemanticFeature feature, ImmutableArray<string> parent)
    {
        var path = parent.Add(feature.Name);
        foreach (var slice in feature.Slices)
        {
            var slicePath = path.Add(slice.Name);
            foreach (var query in slice.Queries)
            {
                yield return new(SemanticAgentOperationKind.Query, query.Id, query.Name, slicePath, query.Delivery == SemanticQueryDelivery.Live);
            }

            foreach (var command in slice.Commands)
            {
                yield return new(SemanticAgentOperationKind.Command, command.Id, command.Name, slicePath, false);
            }
        }

        foreach (var nested in feature.Features.SelectMany(child => Features(child, path))) yield return nested;
    }

    internal sealed record Target(SemanticAgentOperationKind Kind, SemanticId Id, string Name, ImmutableArray<string> Path, bool IsObservable)
    {
        internal string QualifiedName => string.Join('.', Path.Add(Name));
    }
}
