// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    private sealed partial class ValidationContext
    {
        void ValidatePartsProjectionScope(SemanticProjection projection, SemanticReadModel readModel)
        {
            var scope = projection.Scope!;
            RequireObjects(scope.From, nameof(scope.From), "projection transition");
            RequireObjects(scope.Removals, nameof(scope.Removals), "projection removal");
            RequireObjects(scope.Joins, nameof(scope.Joins), "projection join");
            RequireObjects(scope.Children, nameof(scope.Children), "projection children");
            RequireObjects(scope.Nested, nameof(scope.Nested), "projection nested");
            RequireObjects(scope.JoinRemovals, nameof(scope.JoinRemovals), "projection join removal");
            if (!scope.Joins.IsEmpty || !scope.Children.IsEmpty || !scope.Nested.IsEmpty || !scope.JoinRemovals.IsEmpty || scope.Every is not null)
            {
                throw new InvalidSemanticContract("A flat composite-key read model admits root from and remove with blocks only.");
            }
            var keys = ReadModelKeyProperties(readModel);
            var targets = Properties(readModel.Properties);
            var seen = new HashSet<SemanticId>();
            foreach (var from in scope.From)
            {
                RequireUniqueEvent(seen, from.EventContract);
                var sources = EventProperties(from.EventContract);
                ValidatePartsProjectionKey(from.Key, from.ParentKey, keys, sources);
                ValidateProjectionMappings(from.Mappings, targets, sources);
            }
            foreach (var removal in scope.Removals)
            {
                RequireUniqueEvent(seen, removal.EventContract);
                ValidatePartsProjectionKey(removal.Key, removal.ParentKey, keys, EventProperties(removal.EventContract));
            }
        }

        void ValidatePartsProjectionKey(SemanticProjectionKey key, SemanticProjectionKey? parent, SemanticProperty[] keys, Dictionary<SemanticId, SemanticProperty> sources)
        {
            if (!_semanticVersion.IsAtLeast(SemanticVersion.V27) || parent is not null || key is not SemanticProjectionPartsKey { Kind: SemanticProjectionKeyKind.Parts } parts)
            {
                throw new InvalidSemanticContract("A composite read-model projection requires every named key part and no parent key.");
            }
            RequireObjects(parts.Parts, nameof(parts.Parts), "projection key part");
            var seen = new HashSet<SemanticId>();
            foreach (var part in parts.Parts)
            {
                var property = keys.SingleOrDefault(property => property.Id == part.Property);
                if (property is null || !seen.Add(part.Property))
                {
                    throw new InvalidSemanticContract("A projection key contains an unknown or repeated key part.");
                }
                if (part.Value is SemanticProjectionLiteral literal) ValidateValue(literal.Value, property.Type, "projection key part");
                else ValidateKeyValue(part.Value, property.Type, sources, "projection key part");
            }
            if (seen.Count != keys.Length)
            {
                throw new InvalidSemanticContract("A projection key must supply every read-model key part.");
            }
        }
    }
}
