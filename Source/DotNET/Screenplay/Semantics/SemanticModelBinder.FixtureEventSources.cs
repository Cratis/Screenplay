// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        static bool IsEventSourceKey(KeySyntax? key) => key is null or ExpressionKeySyntax { Expression: EventSourceIdExpressionSyntax };

        SemanticTypeReference? FixtureEventSourceType(SemanticTypeReference? producerType, ExpressionSyntax? source, string eventName) =>
            source is null ? producerType : producerType ?? ProjectedEventSourceType(eventName);

        SemanticTypeReference? ProjectedEventSourceType(string eventName)
        {
            var keys = AllSlices().SelectMany(entry => entry.Slice.Projections)
                .Where(projection => projection.ReadModel is not null && projection.Blocks.OfType<FromSyntax>().Any(from =>
                    from.Events.Any(spec => ShortName(spec.Event) == eventName &&
                        (spec.Key is EventSourceIdExpressionSyntax || (spec.Key is null && IsEventSourceKey(from.Key))))))
                .Select(projection => _readModels.GetValueOrDefault(ShortName(projection.ReadModel!)))
                .Select(readModel => readModel?.Model.Properties.Where(property => property.IsIdentifier).ToArray())
                .Select(properties => properties is { Length: 1 } ? properties[0].Type : null)
                .ToArray();
            var types = keys.OfType<SemanticTypeReference>().Distinct().ToArray();
            return keys.Any(type => type is null) || types.Length != 1 || types[0].Kind == SemanticTypeReferenceKind.CompositeType ? null : types[0];
        }
    }
}
