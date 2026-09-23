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
        static bool IsFlatSource(ExpressionSyntax source, Dictionary<string, SemanticProperty> properties) => source switch
        {
            LiteralExpressionSyntax literal => literal.Value is not null,
            PathExpressionSyntax path => IsFlatProperty(path.Path, properties),
            _ => false
        };

        static bool IsFlatProperty(string path, Dictionary<string, SemanticProperty> properties) =>
            !path.Contains('.') && properties.ContainsKey(path);

        SemanticProjection? BindProjection(SemanticAddress slice, ProjectionSyntax projection)
        {
            if (projection.File is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Projection '{projection.Name}' file reference is realization provenance.", projection.File.Location);
            }

            if (projection.Sequence is not null)
            {
                Error(
                    DiagnosticCodes.UnsupportedSemanticSyntax,
                    $"Projection '{projection.Name}' sequence is not portable ESM v1 behavior: which event sequence a projection observes is a realization concern.",
                    projection.Location);
            }

            if (projection.ReadModel is null || !_readModels.TryGetValue(ShortName(projection.ReadModel), out var readModel))
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Projection '{projection.Name}' read model is unresolved.", projection.Location);
                return null;
            }

            var address = SemanticAddress.ForProjection(slice, projection.Name);
            var id = Resolve(address, projection.Location);

            // The flat shape is kept for every projection it can express, so existing consumers read those unchanged.
            if (IsFlat(projection, readModel))
            {
                var transitions = projection.Blocks
                    .Cast<FromSyntax>()
                    .SelectMany(from => from.Events.Select(spec => BindFlatTransition(projection, from, spec, readModel)))
                    .ToImmutableArray();
                return new(id, projection.Name, readModel.Model.Id, transitions);
            }

            var identifier = readModel.Model.Properties.FirstOrDefault(_ => _.IsIdentifier)?.Name ?? string.Empty;
            var root = new ProjectionLevel(projection.Name, readModel.Properties, projection.AutoMap != AutoMapMode.Disabled, true, false, identifier);
            var scope = BindScope(projection.Blocks, root, projection.AutoMap);
            return new(id, projection.Name, readModel.Model.Id, []) { Scope = scope };
        }

        bool IsFlat(ProjectionSyntax projection, BoundReadModel readModel) =>
            projection.Blocks.All(block => block is FromSyntax { ParentKey: null, Key: null or ExpressionKeySyntax } from &&
                from.Events.All(spec => _events.TryGetValue(spec.Event, out var @event) &&
                    (spec.Key ?? (from.Key as ExpressionKeySyntax)?.Expression) is PathExpressionSyntax key && IsFlatProperty(key.Path, @event.Properties) &&
                    from.Mappings.All(mapping => mapping is SetMappingSyntax set &&
                        IsFlatProperty(set.Property, readModel.Properties) &&
                        IsFlatSource(set.Source, @event.Properties))));

        SemanticProjectionTransition BindFlatTransition(
            ProjectionSyntax projection,
            FromSyntax from,
            EventSpecSyntax spec,
            BoundReadModel readModel)
        {
            var @event = _events[spec.Event];

            // Chronicle routes on the inline event key, then the from-block key, then the event source - it never
            // reads a projection-level key (ProjectionDefinitionSyntaxVisitor.ProcessFrom), so neither does ESM.
            var keySyntax = (PathExpressionSyntax)(spec.Key ?? ((ExpressionKeySyntax)from.Key!).Expression);
            var key = SemanticExpression.Property(SemanticExpressionRootKind.Event, @event.Properties[keySyntax.Path].Id);

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

            foreach (var set in from.Mappings.Cast<SetMappingSyntax>())
            {
                var source = set.Source is LiteralExpressionSyntax literal
                    ? SemanticExpression.FromValue(BindLiteral(literal))
                    : SemanticExpression.Property(SemanticExpressionRootKind.Event, @event.Properties[((PathExpressionSyntax)set.Source).Path].Id);
                mappings.Add(new(readModel.Properties[set.Property].Id, source));
            }

            return new(@event.Contract.Id, new(AffectedInstanceCardinality.One, key), mappings.ToImmutable());
        }

        sealed record ProjectionLevel(
            string Projection,
            Dictionary<string, SemanticProperty> Targets,
            bool AutoMap,
            bool IsRoot,
            bool IsChildContext,
            string IdentifierName);
    }
}
