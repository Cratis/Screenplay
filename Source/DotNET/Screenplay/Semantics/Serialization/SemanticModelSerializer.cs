// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Serialization;

/// <summary>
/// Provides strict canonical ESM v1 serialization and reading.
/// </summary>
public static partial class SemanticModelSerializer
{
    /// <summary>
    /// Serializes an executable semantic model to canonical UTF-8 JSON without a BOM or whitespace.
    /// </summary>
    /// <param name="model">The model to serialize.</param>
    /// <returns>The canonical UTF-8 JSON bytes.</returns>
    /// <exception cref="InvalidSemanticContract">The model or revision is invalid.</exception>
    public static byte[] Serialize(ExecutableSemanticModel model)
    {
        if (model is null)
        {
            throw new InvalidSemanticContract("The executable semantic model cannot be null.");
        }

        return SemanticModelCanonicalJson.Serialize(model);
    }
}
