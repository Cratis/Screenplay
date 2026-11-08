// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization.for_EsmJsonSchema.given;

/// <summary>
/// Locates the published ESM JSON Schemas and every ESM document checked into the repository.
/// </summary>
public static class published_esm_schemas
{
    /// <summary>
    /// The environment variable that rewrites the published schemas from the generator when set to <c>1</c>.
    /// </summary>
    public const string RegenerateVariable = "SCREENPLAY_REGENERATE_ESM_SCHEMAS";

    static readonly HashSet<string> _excluded = new(["bin", "obj", "node_modules"], StringComparer.Ordinal);

    /// <summary>
    /// Gets the repository root.
    /// </summary>
    public static string Repository
    {
        get
        {
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (directory is not null && !Path.Exists(Path.Combine(directory.FullName, ".git"))) directory = directory.Parent;
            return directory?.FullName ?? throw new InvalidOperationException("The specs must run inside the Screenplay repository.");
        }
    }

    /// <summary>
    /// Gets the folder the schemas are published in.
    /// </summary>
    public static string Folder => Path.Combine(Repository, "Documentation", "screenplay", "esm");

    /// <summary>
    /// Reads the published schema of a version.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <returns>The schema text.</returns>
    public static string Read(SemanticVersion version) => File.ReadAllText(Path.Combine(Folder, EsmJsonSchema.FileName(version)));

    /// <summary>
    /// Gets every checked-in canonical ESM document of a released version, with its path and version.
    /// </summary>
    /// <returns>The documents.</returns>
    public static IEnumerable<(string Path, SemanticVersion Version, JsonElement Document)> Documents()
    {
        var source = Path.Combine(Repository, "Source", "DotNET");
        foreach (var path in Directory.EnumerateFiles(source, "*.json", SearchOption.AllDirectories).Where(Included).Order(StringComparer.Ordinal))
        {
            JsonElement document;
            try
            {
                using var parsed = JsonDocument.Parse(File.ReadAllBytes(path));
                document = parsed.RootElement.Clone();
            }
            catch (JsonException)
            {
                continue;
            }

            if (document.ValueKind != JsonValueKind.Object || !document.TryGetProperty("schema", out var schema) || schema.ValueKind != JsonValueKind.String || schema.GetString() != "cratis.screenplay.esm") continue;
            if (!document.TryGetProperty("semanticVersion", out var semantic) || !SemanticVersion.TryParse(semantic.GetString(), out var version)) continue;
            if (EsmJsonSchema.Versions.Contains(version)) yield return (Path.GetRelativePath(Repository, path), version, document);
        }
    }

    static bool Included(string path) =>
        !path.Split(Path.DirectorySeparatorChar).Any(_excluded.Contains);
}
#endif
