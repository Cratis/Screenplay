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
        SemanticProjection? BindProjection(SemanticAddress slice, ProjectionSyntax projection)
        {
            if (projection.File is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Projection '{projection.Name}' file reference is realization provenance.", projection.File.Location);
            }

            if (projection.Sequence is not null)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Projection '{projection.Name}' sequence is not portable ESM v1 behavior.", projection.Location);
            }

            if (projection.ReadModel is null || !_readModels.TryGetValue(ShortName(projection.ReadModel), out var readModel))
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Projection '{projection.Name}' read model is unresolved.", projection.Location);
                return null;
            }

            foreach (var block in projection.Blocks.Where(_ => _ is not FromSyntax))
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Projection block '{block.GetType().Name}' is not admitted by the first ESM v1 vertical.", block.Location);
            }

            var address = SemanticAddress.ForProjection(slice, projection.Name);
            var id = Resolve(address, projection.Location);
            var transitions = projection.Blocks
                .OfType<FromSyntax>()
                .Select(value => BindProjectionTransition(projection, value, readModel))
                .Where(_ => _ is not null)
                .Select(_ => _!)
                .ToImmutableArray();
            return new(id, projection.Name, readModel.Model.Id, transitions);
        }

        SemanticProjectionTransition? BindProjectionTransition(
            ProjectionSyntax projection,
            FromSyntax from,
            BoundReadModel readModel)
        {
            var eventSpecs = from.Events.ToArray();
            if (eventSpecs.Length != 1 || !_events.TryGetValue(eventSpecs[0].Event, out var @event))
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Projection '{projection.Name}' transition must name one unambiguous event in the first ESM v1 vertical.", from.Location);
                return null;
            }

            if (from.ParentKey is not null)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Projection '{projection.Name}' parent keys are not admitted by the first ESM v1 vertical.", from.ParentKey.Location);
            }

            var keySyntax = eventSpecs[0].Key ?? ExpressionFrom(from.Key) ?? ExpressionFrom(projection.Key);
            if (keySyntax is null || BindExpression(keySyntax, @event.Properties, SemanticExpressionRootKind.Event, "projection affected key") is not { } key)
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Projection '{projection.Name}' transition requires one resolved affected key.", from.Location);
                return null;
            }

            var mappings = ImmutableArray.CreateBuilder<SemanticPropertyMapping>();
            var explicitlyMapped = from.Mappings.Select(_ => _.Property).ToHashSet(StringComparer.Ordinal);
            if (projection.AutoMap != AutoMapMode.Disabled)
            {
                foreach (var property in readModel.Model.Properties.Where(_ => !explicitlyMapped.Contains(_.Name)))
                {
                    if (@event.Properties.TryGetValue(property.Name, out var source))
                    {
                        mappings.Add(new(property.Id, SemanticExpression.Property(SemanticExpressionRootKind.Event, source.Id)));
                    }
                }
            }

            foreach (var mapping in from.Mappings)
            {
                if (mapping is not SetMappingSyntax set || !readModel.Properties.TryGetValue(mapping.Property, out var target))
                {
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Projection mapping '{mapping.Property}' is not a direct set mapping admitted by ESM v1.", mapping.Location);
                    continue;
                }

                if (BindExpression(set.Source, @event.Properties, SemanticExpressionRootKind.Event, "projection mapping") is { } source)
                {
                    mappings.Add(new(target.Id, source));
                }
            }

            return new(
                @event.Contract.Id,
                new(AffectedInstanceCardinality.One, key),
                mappings.ToImmutable());
        }

        ExpressionSyntax? ExpressionFrom(KeySyntax? key) => key switch
        {
            null => null,
            ExpressionKeySyntax expression => expression.Expression,
            _ => UnsupportedKey(key)
        };

        ExpressionSyntax? UnsupportedKey(KeySyntax key)
        {
            Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Projection key '{key.GetType().Name}' is not admitted by the first ESM v1 vertical.", key.Location);
            return null;
        }
    }
}
