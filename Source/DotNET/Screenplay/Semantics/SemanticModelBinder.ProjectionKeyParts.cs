// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        static bool HasKeyParts(ProjectionLevel level) => level.IsRoot && level.Targets.Values.Count(property => property.IsIdentifier) > 1;

        SemanticProjectionKey RefusePartsRemoval(RemoveWithSyntax removal)
        {
            Error(DiagnosticCodes.UnsupportedSemanticSyntax, "Removal key parts require a read model with several flat key properties.", removal.Location);

            return SemanticProjectionKey.EventSourceIdentity;
        }

        SemanticProjectionKey BindRemovalKey(RemoveWithSyntax removal, BoundEvent @event, ProjectionLevel level)
        {
            if (HasKeyParts(level))
            {
                return BindPartsKey(removal.PartsKey, removal.Key is not null || removal.ParentKey is not null, @event, level, removal.Location);
            }

            return removal.PartsKey is not null ? RefusePartsRemoval(removal) : BindKeyExpression(removal.Key, @event, "removal key");
        }

        SemanticProjectionKey BindFromKey(FromSyntax from, ExpressionSyntax? eventKey, BoundEvent @event, ProjectionLevel level)
        {
            if (HasKeyParts(level))
            {
                return BindPartsKey(from.Key, eventKey is not null || from.ParentKey is not null, @event, level, from.Location);
            }

            return eventKey is not null ? BindKeyExpression(eventKey, @event, "transition key") : BindKey(from.Key, @event);
        }

        SemanticProjectionPartsKey BindPartsKey(KeySyntax? key, bool scalarOrParent, BoundEvent @event, ProjectionLevel level, SourceLocation location)
        {
            var keys = level.Targets.Values.Where(property => property.IsIdentifier).ToArray();
            if (scalarOrParent || key is not CompositeKeySyntax composite)
            {
                Error(DiagnosticCodes.IncompleteReadModelKey, "A composite read-model projection requires an untyped block naming every key part.", location);
                return new SemanticProjectionPartsKey([]);
            }
            if (!string.IsNullOrEmpty(composite.Type))
            {
                Error(DiagnosticCodes.InvalidReadModelKeyLookup, "Flat read-model key parts have no authored composite type.", composite.Location);
            }
            var parts = ImmutableArray.CreateBuilder<SemanticProjectionKeyPart>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var part in composite.Parts)
            {
                var property = keys.SingleOrDefault(property => property.Name == part.Property);
                if (property is null || !seen.Add(part.Property) || part.Expression is not (PathExpressionSyntax or LiteralExpressionSyntax or EventSourceIdExpressionSyntax or EventContextExpressionSyntax))
                {
                    Error(DiagnosticCodes.InvalidReadModelKeyLookup, $"Projection key part '{part.Property}' must name one key property exactly once and use a simple expression.", part.Location);
                    continue;
                }
                if (part.Expression is LiteralExpressionSyntax literal)
                {
                    if (BindConcreteValue(literal, property.Type, "projection key part", false) is { } concrete)
                    {
                        parts.Add(new(property.Id, SemanticProjectionValue.Literal(concrete)));
                    }
                }
                else if (BindProjectionValue(part.Expression, @event, "key part") is { } value)
                {
                    parts.Add(new(property.Id, value));
                }
            }
            var missing = keys.Where(property => !seen.Contains(property.Name)).Select(property => property.Name).ToArray();
            if (missing.Length > 0)
            {
                Error(DiagnosticCodes.IncompleteReadModelKey, $"Projection key is missing parts: {string.Join(", ", missing)}.", composite.Location);
            }

            return new SemanticProjectionPartsKey(parts.ToImmutable());
        }
    }
}
