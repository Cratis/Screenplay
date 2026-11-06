// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Globalization;
using Cratis.Screenplay.Semantics.for_SemanticModelBinder;

namespace Cratis.Screenplay.Semantics.Serialization.given;

/// <summary>
/// Rewrites the current full ESM golden on explicit request; older full goldens are frozen.
/// </summary>
/// <remarks>
/// Regeneration always ends in <see cref="GoldenVectorsRegenerated"/>, so a regenerating run cannot pass.
/// Rebuild embedded resources before rerunning without the request.
/// </remarks>
public static class golden_vector_regeneration
{
    /// <summary>
    /// The environment variable requesting the current golden with <c>1</c> or its version number.
    /// </summary>
    public const string Variable = "SCREENPLAY_REGENERATE_GOLDEN";

    /// <summary>
    /// Gets a value indicating whether regeneration was explicitly requested.
    /// </summary>
    public static bool IsRequested => Environment.GetEnvironmentVariable(Variable) is not (null or "" or "0");

    /// <summary>
    /// Rewrites only the current full ESM golden (and, with <c>1</c>, the non-ESM sidecars).
    /// </summary>
    /// <exception cref="GoldenVectorsRegenerated">The request is refused or regeneration completed.</exception>
    public static void RegenerateWhenRequested()
    {
        RefuseLegacyEventRoutesRegeneration();
        if (IsRequested)
        {
            Regenerate(Environment.GetEnvironmentVariable(Variable)!, frozen_esm_goldens.Directory());
        }
    }

    internal static void RefuseLegacyEventRoutesRegeneration()
    {
        if (Environment.GetEnvironmentVariable("SCREENPLAY_REGENERATE_EVENT_ROUTES") == "1")
        {
            throw new GoldenVectorsRegenerated("ESM v8 full golden is frozen. SCREENPLAY_REGENERATE_EVENT_ROUTES cannot rewrite older goldens.");
        }
    }

    internal static void Regenerate(string request, string directory)
    {
        var current = frozen_esm_goldens.CurrentVersion;
        if (request != "1" && int.TryParse(request, NumberStyles.None, CultureInfo.InvariantCulture, out var version) && frozen_esm_goldens.Sha256.ContainsKey(version))
        {
            throw new GoldenVectorsRegenerated($"ESM v{version} full golden is frozen. Older goldens cannot be regenerated; only the current ESM v{current} golden may be rewritten.");
        }
        if (request != "1" && request != current.ToString())
        {
            throw new GoldenVectorsRegenerated($"Unknown golden regeneration request '{request}'. Use {Variable}=1 or {current} for the current full ESM golden.");
        }
        if (!Directory.Exists(directory))
        {
            throw new GoldenVectorsRegenerated($"The golden vector directory '{directory}' does not exist, so nothing was regenerated. Regenerate from a source checkout, not from a build with mapped source paths.");
        }

        var model = canonical_serialization_golden_vectors.CreateSemanticModelV41();
        if (model.SemanticVersion != current)
        {
            throw new GoldenVectorsRegenerated($"The current golden builder must select ESM v{current}; nothing was regenerated.");
        }
        var written = new List<string> { Write(directory, current.Minor == 0 ? $"full-esm-v{current.Major}.json" : $"full-esm-v{current}.json", SemanticModelSerializer.Serialize(model)) };
        if (request == "1")
        {
            written.Add(Write(directory, "full-expressions-v1.json", SemanticModelCanonicalJson.SerializeExpressionVector(canonical_serialization_golden_vectors.CreateExpressions())));
            written.Add(Write(directory, "full-identity-catalog-v1.json", SemanticIdentityCatalogSerializer.Serialize(canonical_serialization_golden_vectors.CreateIdentityCatalog())));
            written.Add(Write(directory, "typed-contexts-v1.json", when_describing_typed_contexts.GoldenBytes()));
            written.Add(Write(directory, "unbound-handler-context-v1.json", when_describing_an_unbound_handler.GoldenBytes()));
            written.Add(Write(directory, "unbound-handler-context-v3.json", for_SemanticModelBinder.when_binding_clock_operands.with_a_declared_context_member.GoldenBytes()));
        }
        throw new GoldenVectorsRegenerated($"Golden vectors regenerated ({string.Join(", ", written)}) in '{directory}'. Older full ESM goldens remain frozen. Review the diff, then rebuild and rerun without {Variable}.");
    }

    static string Write(string directory, string name, byte[] bytes)
    {
        File.WriteAllBytes(Path.Combine(directory, name), bytes);

        return name;
    }
}
#endif
