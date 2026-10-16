// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        ImmutableArray<SemanticQueryKeyPart> BindQueryKeyParts(SemanticAddress address, QuerySyntax query, BoundReadModel readModel)
        {
            if (!query.ByParts.Any()) return [];
            var keys = readModel.Model.Properties.Where(property => property.IsIdentifier).ToArray();
            var parts = ImmutableArray.CreateBuilder<SemanticQueryKeyPart>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var part in query.ByParts)
            {
                var target = keys.SingleOrDefault(property => property.Name == part.Name);
                var type = BindTypeReference(part.Type);
                if (!seen.Add(part.Name) || target is null || type.IsCollection || type.IsOptional || type != target.Type)
                {
                    Error(DiagnosticCodes.InvalidReadModelKeyLookup, $"Query key part '{part.Name}' must name a key property with its required compatible type, exactly once.", part.Location);
                    continue;
                }
                parts.Add(new(Resolve(SemanticAddress.ForQueryArgument(address, part.Name), part.Location), part.Name, type, target.Id));
            }
            var missing = keys.Where(property => !seen.Contains(property.Name)).Select(property => property.Name).ToArray();
            if (missing.Length > 0)
            {
                Error(DiagnosticCodes.IncompleteReadModelKey, $"Read-model lookup is missing key parts: {string.Join(", ", missing)}.", query.Location);
            }

            return parts.ToImmutable();
        }
    }
}
