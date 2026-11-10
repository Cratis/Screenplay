// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    internal static SemanticTypeReference DeclaredFixtureEventSourceType(
        IEnumerable<SemanticCommand> commands,
        IEnumerable<SemanticReaction> reactions,
        IEnumerable<SemanticCapture> captures,
        IEnumerable<SemanticProjection> projections,
        IEnumerable<SemanticReadModel> readModels,
        SemanticId eventContract)
    {
        var projectionArray = projections.ToArray();
        var producerTypes = DeclaredEventSourceTypes(commands, reactions, captures, projectionArray, eventContract);
        if (producerTypes.Length == 1) return producerTypes[0];

        return ProjectedFixtureSourceType(projectionArray, readModels, eventContract)
            ?? DeclaredEventSourceType(commands, reactions, captures, projectionArray, eventContract);
    }

    static SemanticTypeReference? ProjectedFixtureSourceType(
        IEnumerable<SemanticProjection> projections,
        IEnumerable<SemanticReadModel> readModels,
        SemanticId eventContract)
    {
        var models = readModels.ToDictionary(model => model.Id);
        var keys = projections.Where(projection => ProjectsEventSourceIdentity(projection, eventContract))
            .Select(projection => models.GetValueOrDefault(projection.ReadModel)?.Properties.Where(property => property.IsIdentifier).ToArray())
            .Select(properties => properties is { Length: 1 } ? properties[0].Type : null)
            .ToArray();
        if (keys.Any(type => type is not { IsOptional: false, IsCollection: false, Kind: not SemanticTypeReferenceKind.CompositeType })) return null;
        var types = keys.OfType<SemanticTypeReference>().Distinct().ToArray();
        return types.Length == 1 ? types[0] : null;
    }

    static bool ProjectsEventSourceIdentity(SemanticProjection projection, SemanticId eventContract) =>
        projection.Target == SemanticProjectionTargetKind.ReadModel && projection.Scope?.From.Any(from =>
            from.EventContract == eventContract && from.Key == SemanticProjectionKey.EventSourceIdentity) == true;
}
