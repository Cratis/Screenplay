// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Serialization.given;

public static partial class canonical_serialization_golden_vectors
{
    const string V4Resource = "Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v4.json";

    public static byte[] SemanticModelV4Bytes => ReadResource(V4Resource);

    public static ExecutableSemanticModel CreateSemanticModelV4()
    {
        var model = CreateSemanticModelV3();
        var modules = model.Application.Modules.Select(module => module with
        {
            Features = [.. module.Features.Select(root => root with
            {
                Features = [.. root.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice with
                    {
                        Events = [.. slice.Events.Select(@event => @event.Name == "EntityCreated" ? @event with
                        {
                            Revision = new(3),
                            Predecessor = new(2),
                            PriorRevisions =
                            [
                                new(new(1), null, [.. @event.Properties.Select((property, index) => property with { Id = Id(4000 + index) })])
                                { Tags = ["historical", "billing"] },
                                new(new(2), new(1), [.. @event.Properties.Select((property, index) => property with { Id = Id(5000 + index) })])
                                { Tags = ["intermediate"] }
                            ]
                        } : @event)],
                        Projections = [.. slice.Projections.Select(projection => projection with
                        {
                            Transitions = [.. projection.Transitions.Where(transition => transition.AffectedInstance.Cardinality == AffectedInstanceCardinality.One)]
                        })]
                    })]
                })]
            })]
        }).ToImmutableArray();
        return ExecutableSemanticModel.Create(LanguageVersion.V4, SemanticVersion.V4, model.Application with { Modules = modules });
    }
}
#endif
