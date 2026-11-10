// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Syntax.Specifications;

internal static class ProjectedEventSourceIdentity
{
    internal static TypeRefSyntax? TypeFor(ApplicationSyntax application, string eventName)
    {
        var slices = application.Modules.SelectMany(module => module.Features).SelectMany(Slices).ToArray();
        var readModels = slices.SelectMany(owner => (owner.ReadModels ?? []).Select(model => (Owner: owner, Model: model))).ToArray();
        var keys = slices.SelectMany(slice => slice.Projections)
            .Where(projection => ProjectsEventSource(projection, eventName))
            .Select(projection => IdentityType(readModels, projection)).ToArray();
        if (keys.Any(type => type is null or { IsOptional: true } or { IsCollection: true })) return null;
        var types = keys.OfType<TypeRefSyntax>().DistinctBy(type => type.Name).ToArray();
        return types is [var identity] && !(application.Types ?? []).Any(type => type.Name == identity.Name) ? identity : null;
    }

    static IEnumerable<SliceSyntax> Slices(FeatureSyntax feature) => feature.Slices.Concat(feature.Features.SelectMany(Slices));

    static bool ProjectsEventSource(ProjectionSyntax projection, string eventName) => projection.ReadModel is not null &&
        projection.Blocks.OfType<FromSyntax>().Any(from => from.Events.Any(spec => spec.Event.Split('.')[^1] == eventName &&
            (spec.Key is EventSourceIdExpressionSyntax || (spec.Key is null && IsEventSourceKey(from.Key)))));

    static bool IsEventSourceKey(KeySyntax? key) => key is null or ExpressionKeySyntax { Expression: EventSourceIdExpressionSyntax };

    static TypeRefSyntax? IdentityType((SliceSyntax Owner, ReadModelSyntax Model)[] readModels, ProjectionSyntax projection)
    {
        var declaration = readModels.FirstOrDefault(entry => entry.Model.Name == projection.ReadModel!.Split('.')[^1]);
        if (declaration.Model is null) return null;
        var names = ReadModelIdentity.IdentifierNames(declaration.Owner, declaration.Model);
        return names is [var name] ? declaration.Model.Properties.SingleOrDefault(property => property.Name == name)?.Type : null;
    }
}
