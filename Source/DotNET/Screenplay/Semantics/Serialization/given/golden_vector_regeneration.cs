// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Runtime.CompilerServices;
using Cratis.Screenplay.Semantics.for_SemanticModelBinder;

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

    static readonly IReadOnlyDictionary<string, Func<ExecutableSemanticModel>> _versionBuilders = new Dictionary<string, Func<ExecutableSemanticModel>>(StringComparer.Ordinal)
    {
        ["3"] = canonical_serialization_golden_vectors.CreateSemanticModelV3,
        ["4"] = canonical_serialization_golden_vectors.CreateSemanticModelV4,
        ["5"] = canonical_serialization_golden_vectors.CreateSemanticModelV5,
        ["6"] = canonical_serialization_golden_vectors.CreateSemanticModelV6,
        ["7"] = canonical_serialization_golden_vectors.CreateSemanticModelV7,
        ["8"] = canonical_serialization_golden_vectors.CreateSemanticModelV8,
        ["9"] = canonical_serialization_golden_vectors.CreateSemanticModelV9,
        ["10"] = canonical_serialization_golden_vectors.CreateSemanticModelV10,
        ["11"] = canonical_serialization_golden_vectors.CreateSemanticModelV11,
        ["12"] = canonical_serialization_golden_vectors.CreateSemanticModelV12,
        ["13"] = canonical_serialization_golden_vectors.CreateSemanticModelV13,
        ["14"] = canonical_serialization_golden_vectors.CreateSemanticModelV14,
        ["15"] = canonical_serialization_golden_vectors.CreateSemanticModelV15,
        ["16"] = canonical_serialization_golden_vectors.CreateSemanticModelV16,
        ["17"] = canonical_serialization_golden_vectors.CreateSemanticModelV17,
        ["18"] = canonical_serialization_golden_vectors.CreateSemanticModelV18,
        ["19"] = canonical_serialization_golden_vectors.CreateSemanticModelV19,
        ["20"] = canonical_serialization_golden_vectors.CreateSemanticModelV20,
        ["21"] = canonical_serialization_golden_vectors.CreateSemanticModelV21,
        ["22"] = canonical_serialization_golden_vectors.CreateSemanticModelV22,
        ["23"] = canonical_serialization_golden_vectors.CreateSemanticModelV23,
        [TextRuleVersion.Major.ToString(System.Globalization.CultureInfo.InvariantCulture)] = canonical_serialization_golden_vectors.CreateSemanticModelV24
    };

    /// <summary>
    /// Gets a value indicating whether regeneration was explicitly requested.
    /// </summary>
    public static bool IsRequested => Environment.GetEnvironmentVariable(Variable) == "1" || Environment.GetEnvironmentVariable(Variable) == "3" || Environment.GetEnvironmentVariable(Variable) == "4" || Environment.GetEnvironmentVariable(Variable) == "5" || Environment.GetEnvironmentVariable(Variable) == "6" || Environment.GetEnvironmentVariable(Variable) == "7" || Environment.GetEnvironmentVariable(Variable) == "8" || Environment.GetEnvironmentVariable(Variable) == "9" || Environment.GetEnvironmentVariable(Variable) == "10" || Environment.GetEnvironmentVariable(Variable) == "11" || Environment.GetEnvironmentVariable(Variable) == "12" || Environment.GetEnvironmentVariable(Variable) == "13" || Environment.GetEnvironmentVariable(Variable) == "14" || Environment.GetEnvironmentVariable(Variable) == "15" || Environment.GetEnvironmentVariable(Variable) == "16" || Environment.GetEnvironmentVariable(Variable) == "17" || Environment.GetEnvironmentVariable(Variable) == "18" || Environment.GetEnvironmentVariable(Variable) == "19" || Environment.GetEnvironmentVariable(Variable) == "20" || Environment.GetEnvironmentVariable(Variable) == "21" || Environment.GetEnvironmentVariable(Variable) == "22" || Environment.GetEnvironmentVariable(Variable) == "23" || Environment.GetEnvironmentVariable(Variable) == TextRuleVersion.Major.ToString(System.Globalization.CultureInfo.InvariantCulture);

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

        var version = Environment.GetEnvironmentVariable(Variable);
        if (VersionBuilder(version) is { } build)
        {
            var name = Write(directory, $"full-esm-v{version}.json", SemanticModelSerializer.Serialize(build()));
            throw new GoldenVectorsRegenerated($"Golden vector regenerated ({name}) in '{directory}'. Review the diff, then rebuild and rerun without {Variable}.");
        }

        var written = new[]
        {
            Write(directory, "full-esm-v1.json", SemanticModelSerializer.Serialize(canonical_serialization_golden_vectors.CreateSemanticModel())),
            Write(directory, "full-esm-v2.json", SemanticModelSerializer.Serialize(canonical_serialization_golden_vectors.CreateSemanticModelV2())),
            Write(directory, "full-esm-v3.json", SemanticModelSerializer.Serialize(canonical_serialization_golden_vectors.CreateSemanticModelV3())),
            Write(directory, "full-esm-v4.json", SemanticModelSerializer.Serialize(canonical_serialization_golden_vectors.CreateSemanticModelV4())),
            Write(directory, "full-esm-v5.json", SemanticModelSerializer.Serialize(canonical_serialization_golden_vectors.CreateSemanticModelV5())),
            Write(directory, "full-esm-v6.json", SemanticModelSerializer.Serialize(canonical_serialization_golden_vectors.CreateSemanticModelV6())),
            Write(directory, "full-esm-v7.json", SemanticModelSerializer.Serialize(canonical_serialization_golden_vectors.CreateSemanticModelV7())),
            Write(directory, "full-expressions-v1.json", SemanticModelCanonicalJson.SerializeExpressionVector(canonical_serialization_golden_vectors.CreateExpressions())),
            Write(directory, "full-identity-catalog-v1.json", SemanticIdentityCatalogSerializer.Serialize(canonical_serialization_golden_vectors.CreateIdentityCatalog())),
            Write(directory, "typed-contexts-v1.json", when_describing_typed_contexts.GoldenBytes()),
            Write(directory, "unbound-handler-context-v1.json", when_describing_an_unbound_handler.GoldenBytes())
        };

        throw new GoldenVectorsRegenerated(
            $"Golden vectors regenerated ({string.Join(", ", written)}) in '{directory}'. " +
            $"Review the diff - every revision hash inside changes with the model - then rebuild and rerun without {Variable}.");
    }

    static Func<ExecutableSemanticModel>? VersionBuilder(string? version) =>
        _versionBuilders.TryGetValue(version ?? string.Empty, out var builder) ? builder : null;

    static string Write(string directory, string name, byte[] bytes)
    {
        File.WriteAllBytes(Path.Combine(directory, name), bytes);
        return name;
    }

    static string GoldenDirectory([CallerFilePath] string path = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, "..", "Golden"));
}
#endif
