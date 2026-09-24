// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Serialization.given;

public static partial class canonical_serialization_golden_vectors
{
    const string V3Resource = "Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v3.json";

    public static byte[] SemanticModelV3Bytes => ReadResource(V3Resource);

    public static ExecutableSemanticModel CreateSemanticModelV3()
    {
        var model = CreateSemanticModelV2();
        var module = model.Application.Modules.Single();
        var root = module.Features.Single();
        var feature = root.Features.Single();
        var events = feature.Slices.Single(slice => slice.Name == "Creation").Events;
        var eventContract = events.Single(value => value.Name == "EntityCreated").Id;
        var otherEvent = events.Single(value => value.Name == "EntityMaybeSelected").Id;
        var readModel = new SemanticReadModel(
            Id(3000),
            "EntityCount",
            [
                new(Id(3001), "Id", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Uuid), true),
                new(Id(3002), "Count", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.WholeNumber), false)]);
        var reducer = new SemanticReducer(
            "EntityCountReducer",
            readModel.Id,
            [new(otherEvent, new string('b', 64)), new(eventContract, new string('a', 64))]);
        var slices = feature.Slices.Select(slice => slice.Name == "EntitySummaries"
            ? slice with { ReadModels = slice.ReadModels.Add(readModel), Reducers = [reducer] }
            : slice).ToImmutableArray();
        var application = model.Application with
        {
            Modules = [module with { Features = [root with { Features = [feature with { Slices = slices }] }] }]
        };
        return ExecutableSemanticModel.Create(LanguageVersion.V3, SemanticVersion.V3, application);
    }
}
#endif
