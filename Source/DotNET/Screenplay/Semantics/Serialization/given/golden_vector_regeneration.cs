// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Runtime.CompilerServices;

namespace Cratis.Screenplay.Semantics.Serialization.given;

/// <summary>
/// Rewrites the checked-in golden vectors from their source models, but only on explicit request.
/// </summary>
/// <remarks>
/// Regeneration is opt-in through the <see cref="Variable"/> environment variable and always ends in
/// <see cref="GoldenVectorsRegenerated"/>, so a run that regenerates can never pass - in CI or anywhere else.
/// The golden bytes are embedded at build time, so the rerun must rebuild before it compares them.
/// </remarks>
public static class golden_vector_regeneration
{
    /// <summary>
    /// The environment variable that requests regeneration when set to <c>1</c>.
    /// </summary>
    public const string Variable = "SCREENPLAY_REGENERATE_GOLDEN";

    /// <summary>
    /// Gets a value indicating whether regeneration was explicitly requested.
    /// </summary>
    public static bool IsRequested => Environment.GetEnvironmentVariable(Variable) == "1";

    /// <summary>
    /// Rewrites every golden vector from its source model when regeneration was requested.
    /// </summary>
    /// <exception cref="GoldenVectorsRegenerated">Always thrown after the vectors are rewritten.</exception>
    public static void RegenerateWhenRequested()
    {
        if (!IsRequested)
        {
            return;
        }

        var directory = GoldenDirectory();
        if (!Directory.Exists(directory))
        {
            throw new GoldenVectorsRegenerated($"The golden vector directory '{directory}' does not exist, so nothing was regenerated. Regenerate from a source checkout, not from a build with mapped source paths.");
        }

        var written = new[]
        {
            Write(directory, "full-esm-v1.json", SemanticModelSerializer.Serialize(canonical_serialization_golden_vectors.CreateSemanticModel())),
            Write(directory, "full-esm-v2.json", SemanticModelSerializer.Serialize(canonical_serialization_golden_vectors.CreateSemanticModelV2())),
            Write(directory, "full-expressions-v1.json", SemanticModelCanonicalJson.SerializeExpressionVector(canonical_serialization_golden_vectors.CreateExpressions())),
            Write(directory, "full-identity-catalog-v1.json", SemanticIdentityCatalogSerializer.Serialize(canonical_serialization_golden_vectors.CreateIdentityCatalog()))
        };

        throw new GoldenVectorsRegenerated(
            $"Golden vectors regenerated ({string.Join(", ", written)}) in '{directory}'. " +
            $"Review the diff - every revision hash inside changes with the model - then rebuild and rerun without {Variable}.");
    }

    static string Write(string directory, string name, byte[] bytes)
    {
        File.WriteAllBytes(Path.Combine(directory, name), bytes);
        return name;
    }

    static string GoldenDirectory([CallerFilePath] string path = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, "..", "Golden"));
}
#endif
