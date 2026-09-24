// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Serialization.given;

public static partial class canonical_serialization_golden_vectors
{
    const string V2Resource = "Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v2.json";

    public static byte[] SemanticModelV2Bytes => ReadResource(V2Resource);

    public static ExecutableSemanticModel CreateSemanticModelV2()
    {
        var model = CreateSemanticModel();
        var feature = model.Application.Modules.Single().Features.Single().Features.Single();
        var slices = feature.Slices.Select(slice =>
        {
            if (slice.Name != "Creation") return slice;

            var command = slice.Commands.Single();
            var destination = command.Properties.Single(property => property.Name == "Destination");
            var eventContract = slice.Events.Single(@event => @event.Name == "EntityCreated");
            var label = eventContract.Properties.Single(property => property.Name == "Label");
            var produced = command.Produces.Select(value => value.EventContract != eventContract.Id ? value : value with
            {
                Mappings = [.. value.Mappings.Select(mapping => mapping.TargetProperty != label.Id ? mapping : mapping with
                {
                    Source = new SemanticEventContextExpression(SemanticEventContextValueKind.CausedByName, label.Type)
                })]
            }).ToImmutableArray();
            var identity = new SemanticEventSourceIdentity(destination.Type, SemanticValue.Text("00000000-0000-0000-0000-000000000123"));
            var specifications = slice.Specifications.Select(specification => specification.Name != "creates an entity from existing state" ? specification : specification with
            {
                When = specification.When! with { EventSource = identity },
                GivenEvents = [.. specification.GivenEvents.Select(value => value with { EventSource = identity })],
                ThenEvents = [.. specification.ThenEvents.Select(value => value with { EventSource = identity })]
            }).ToImmutableArray();
            return slice with
            {
                Commands = [command with
                {
                    Destination = new(destination.Type, SemanticExpression.Property(SemanticExpressionRootKind.Command, destination.Id)),
                    Produces = produced
                }],
                Specifications = specifications
            };
        }).ToImmutableArray();
        var updatedFeature = feature with { Slices = slices };
        var module = model.Application.Modules.Single();
        var root = module.Features.Single();
        var application = model.Application with
        {
            Modules = [module with { Features = [root with { Features = [updatedFeature] }] }]
        };
        return ExecutableSemanticModel.Create(LanguageVersion.V2, SemanticVersion.V2, application);
    }
}
#endif
