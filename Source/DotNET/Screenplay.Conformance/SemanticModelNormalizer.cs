// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Linq;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Conformance;

/// <summary>
/// Normalises a canonical semantic model so two models can be compared independently of how their identities were assigned.
/// </summary>
/// <remarks>
/// <para>N1: only the canonical JSON is read; source spans, documents, the identity catalog and the source map are not in it.</para>
/// <para>N2: the top-level <c language="text">revision</c> and any catalog revision are dropped; everything else is kept.</para>
/// <para>N3: identities become structural keys (see <see cref="SemanticStructuralKeys"/>); an identity with no definition fails.</para>
/// <para>N4: arrays the writer sorts are compared as sets; every other array keeps its order (see <see cref="SemanticArrayClassification"/>).</para>
/// <para>N5: strings and numbers keep their exact canonical text.</para>
/// </remarks>
internal static class SemanticModelNormalizer
{
    /// <summary>
    /// Normalises a semantic model.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <returns>The normalised model.</returns>
    /// <exception cref="SemanticNormalizationFailed">The model holds an identity with no definition, or an array that is not classified.</exception>
    public static NormalizedSemanticModel Normalize(ExecutableSemanticModel model) =>
        NormalizeCanonicalJson(SemanticModelSerializer.Serialize(model));

    /// <summary>
    /// Normalises the canonical JSON <see cref="SemanticModelSerializer"/> writes, without validating it as a model.
    /// </summary>
    /// <param name="canonicalJson">The canonical JSON.</param>
    /// <returns>The normalised model.</returns>
    /// <exception cref="SemanticNormalizationFailed">The JSON holds an identity with no definition, or an array that is not classified.</exception>
    public static NormalizedSemanticModel NormalizeCanonicalJson(ReadOnlyMemory<byte> canonicalJson)
    {
        using var document = JsonDocument.Parse(canonicalJson);
        var keys = SemanticStructuralKeys.Assign(document.RootElement);
        var builder = new SemanticNodeBuilder(keys);
        var root = builder.Root(document.RootElement);
        return new(root, [.. builder.FallbackKeys.Order(StringComparer.Ordinal)]);
    }
}
