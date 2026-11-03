// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Identifies read-model reducers whose single key can be read with ReducerContext.KeyAs&lt;T&gt;().
/// </summary>
public static class SemanticReducerKeyConversion
{
    /// <summary>
    /// Gets the declared key type that Stage and other renderers may admit for KeyAs&lt;T&gt;().
    /// </summary>
    /// <param name="application">The application containing the reducer and its target.</param>
    /// <param name="reducer">The reducer to examine.</param>
    /// <returns>The original single Uuid or Text key reference, or null when the body must use Key.</returns>
    public static SemanticTypeReference? KeyType(SemanticApplication application, SemanticReducer reducer)
    {
        if (reducer.Target != SemanticProjectionTargetKind.ReadModel)
        {
            return null;
        }

        var slices = application.Modules.SelectMany(module => module.Features.SelectMany(AllSlices)).ToArray();
        var readModel = slices.SelectMany(slice => slice.ReadModels).FirstOrDefault(candidate => candidate.Id == reducer.ReadModel);
        var identifiers = readModel?.Properties.Where(property => property.IsIdentifier).ToArray() ?? [];
        if (identifiers.Length != 1)
        {
            return null;
        }

        var type = identifiers[0].Type;
        if (type.IsOptional || type.IsCollection)
        {
            return null;
        }

        var primitive = type.Kind switch
        {
            SemanticTypeReferenceKind.Primitive => type.Primitive,
            SemanticTypeReferenceKind.Concept => application.Concepts.FirstOrDefault(concept => concept.Id == type.Target)?.Primitive ?? SemanticPrimitiveType.Unknown,
            _ => SemanticPrimitiveType.Unknown
        };
        return primitive is SemanticPrimitiveType.Uuid or SemanticPrimitiveType.Text ? type : null;
    }

    static IEnumerable<SemanticSlice> AllSlices(SemanticFeature feature) => feature.Slices.Concat(feature.Features.SelectMany(AllSlices));
}
