// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

internal static class SemanticReadModelPropertyOrder
{
    internal static ImmutableArray<SemanticId> FromDeclaration(ImmutableArray<SemanticProperty> properties)
    {
        var declared = properties.Select(property => property.Id).ToArray();
        return declared.SequenceEqual(Canonical(properties)) ? [] : [.. declared];
    }

    internal static SemanticApplication Apply(SemanticApplication application) => application with
    {
        Modules = [.. application.Modules.Select(module => module with { Features = [.. module.Features.Select(Apply)] })]
    };

    internal static bool IsUsed(SemanticApplication application) => Models(application).Any(model => !model.PropertyOrder.IsDefaultOrEmpty);

    internal static void ValidateVersion(SemanticApplication application, SemanticVersion version, int utcAssessmentYear)
    {
        if (IsUsed(application) && version != SemanticVersion.V36_1)
        {
            throw new InvalidSemanticContract("Read-model property order requires ESM v36.1.");
        }
        if (version == SemanticVersion.V36_1)
        {
            SemanticModelValidator.Validate(Remove(application), SemanticVersion.V36, utcAssessmentYear);
        }
    }

    internal static void Validate(SemanticReadModel model)
    {
        if (model.PropertyOrder.IsDefault) throw new InvalidSemanticContract("Read-model property order cannot be default.");
        if (model.PropertyOrder.IsEmpty) return;
        var canonical = Canonical(model.Properties).ToArray();
        if (model.PropertyOrder.Length != canonical.Length || !model.PropertyOrder.OrderBy(id => id.ToString(), StringComparer.Ordinal).SequenceEqual(canonical))
        {
            throw new InvalidSemanticContract("Read-model property order must contain every property identity exactly once.");
        }
        if (model.PropertyOrder.SequenceEqual(canonical)) throw new InvalidSemanticContract("Read-model property order must be omitted when it is canonical identity order.");
    }

    static SemanticFeature Apply(SemanticFeature feature) => feature with
    {
        Features = [.. feature.Features.Select(Apply)],
        Slices = [.. feature.Slices.Select(slice => slice with
        {
            ReadModels = [.. slice.ReadModels.Select(model => model with { PropertyOrder = FromDeclaration(model.Properties) })]
        })]
    };

    static SemanticApplication Remove(SemanticApplication application) => application with
    {
        Modules = [.. application.Modules.Select(module => module with { Features = [.. module.Features.Select(Remove)] })]
    };

    static SemanticFeature Remove(SemanticFeature feature) => feature with
    {
        Features = [.. feature.Features.Select(Remove)],
        Slices = [.. feature.Slices.Select(slice => slice with { ReadModels = [.. slice.ReadModels.Select(model => model with { PropertyOrder = [] })] })]
    };

    static IEnumerable<SemanticId> Canonical(ImmutableArray<SemanticProperty> properties) => properties.Select(property => property.Id).OrderBy(id => id.ToString(), StringComparer.Ordinal);

    static IEnumerable<SemanticReadModel> Models(SemanticApplication application) => application.Modules.SelectMany(module => module.Features).SelectMany(Models);

    static IEnumerable<SemanticReadModel> Models(SemanticFeature feature) => feature.Slices.SelectMany(slice => slice.ReadModels).Concat(feature.Features.SelectMany(Models));
}
