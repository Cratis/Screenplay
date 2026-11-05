// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.given;

internal static class property_order_projection
{
    internal static byte[] Bytes(EsmMinorGoldenVector vector)
    {
        var model = SemanticModelSerializer.Deserialize(vector.Bytes.AsSpan());
        var application = model.Application with
        {
            Modules = [.. model.Application.Modules.Select(module => module with { Features = [.. module.Features.Select(Remove)] })]
        };
        return SemanticModelSerializer.Serialize(ExecutableSemanticModel.Create(LanguageVersion.V37, SemanticVersion.V37, application, 2026));
    }

    static SemanticFeature Remove(SemanticFeature feature) => feature with
    {
        Features = [.. feature.Features.Select(Remove)],
        Slices = [.. feature.Slices.Select(slice => slice with { ReadModels = [.. slice.ReadModels.Select(model => model with { PropertyOrder = [] })] })]
    };
}
