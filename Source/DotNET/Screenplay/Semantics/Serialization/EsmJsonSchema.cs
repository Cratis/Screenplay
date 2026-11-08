// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Screenplay.Semantics.Serialization;

/// <summary>
/// Generates the published JSON Schema (draft 2020-12) for each released version of the canonical executable semantic model.
/// </summary>
/// <remarks>
/// The schema is generated from the frozen ESM vocabulary the strict reader uses, so it names exactly the members a
/// version writes, how they nest and what kind of JSON value each holds. It does not repeat the reader's semantic
/// rules (identifier formats, revisions, cross references); a document that validates is well-formed, not admitted.
/// </remarks>
public static class EsmJsonSchema
{
    /// <summary>
    /// The address the published schemas are downloaded from; each schema's <c>$id</c> is this followed by its <see cref="FileName"/>.
    /// </summary>
    public const string BaseUrl = "https://raw.githubusercontent.com/Cratis/Screenplay/main/Documentation/screenplay/esm/";

    /// <summary>
    /// Gets every released ESM version a schema is published for, in ascending order.
    /// </summary>
    public static ImmutableArray<SemanticVersion> Versions { get; } =
        [.. EsmSchemaV41Support.SemanticVersions.Where(version => EsmBaselineShapes.Majors.Contains(version.Major)).OrderBy(version => version.Major).ThenBy(version => version.Minor)];

    static JsonSerializerOptions Options { get; } = new() { WriteIndented = true };

    /// <summary>
    /// Gets the published file name of a version's schema, <c>esm-v41.schema.json</c> for 41.0 and <c>esm-v37.1.schema.json</c> for 37.1.
    /// </summary>
    /// <param name="version">A version listed in <see cref="Versions"/>.</param>
    /// <returns>The file name.</returns>
    public static string FileName(SemanticVersion version) => version.Minor == 0
        ? $"esm-v{version.Major}.schema.json"
        : $"esm-v{version.Major}.{version.Minor}.schema.json";

    /// <summary>
    /// Generates the JSON Schema for a released version as indented UTF-8 JSON text with LF line endings.
    /// </summary>
    /// <param name="version">A version listed in <see cref="Versions"/>.</param>
    /// <returns>The schema document.</returns>
    /// <exception cref="InvalidSemanticContract">No schema is published for the version.</exception>
    public static string For(SemanticVersion version)
    {
        if (!Versions.Contains(version)) throw new InvalidSemanticContract($"No ESM JSON Schema is published for version '{version}'.");

        var definitions = new SortedDictionary<string, JsonNode>(StringComparer.Ordinal);
        var pending = new Queue<EsmBaselineShape>([EsmBaselineShape.Application]);
        while (pending.TryDequeue(out var shape))
        {
            var name = shape.ToString();
            if (definitions.ContainsKey(name)) continue;
            definitions[name] = Shape(version, shape, pending);
        }

        var root = new JsonObject
        {
            ["$schema"] = "https://json-schema.org/draft/2020-12/schema",
            ["$id"] = BaseUrl + FileName(version),
            ["title"] = $"Cratis Screenplay executable semantic model {version.Major}.{version.Minor}",
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["required"] = new JsonArray("schema", "schemaVersion", "languageVersion", "semanticVersion", "revision", "application"),
            ["properties"] = new JsonObject
            {
                ["schema"] = new JsonObject { ["const"] = "cratis.screenplay.esm" },
                ["schemaVersion"] = new JsonObject { ["const"] = version.Major },
                ["languageVersion"] = new JsonObject { ["type"] = "string" },
                ["semanticVersion"] = new JsonObject { ["const"] = $"{version.Major}.{version.Minor}" },
                ["revision"] = new JsonObject { ["type"] = "string" },
                ["application"] = Reference(EsmBaselineShape.Application)
            },
            ["$defs"] = new JsonObject(definitions.Select(pair => KeyValuePair.Create(pair.Key, (JsonNode?)pair.Value)))
        };

        return root.ToJsonString(Options).ReplaceLineEndings("\n") + "\n";
    }

    static JsonObject Shape(SemanticVersion version, EsmBaselineShape shape, Queue<EsmBaselineShape> pending)
    {
        var fields = EsmBaselineShapes.Fields(version.Major, shape);
        var properties = new JsonObject();
        foreach (var (member, kind) in EsmSchemaMembers.For(shape))
        {
            if (fields.TryGetValue(member, out var child))
            {
                properties[member] = child == EsmBaselineShape.Atom ? EsmJsonSchemaKinds.Atom(kind) : Nested(shape, member, child, kind, pending);
            }
            else if (EsmMinorMembers.IsPromoted(version.Major, version.Minor, shape, member))
            {
                properties[member] = EsmJsonSchemaKinds.Atom(kind);
            }
        }

        return new JsonObject { ["type"] = "object", ["additionalProperties"] = false, ["properties"] = properties };
    }

    static JsonObject Nested(EsmBaselineShape shape, string member, EsmBaselineShape child, string kind, Queue<EsmBaselineShape> pending)
    {
        pending.Enqueue(child);

        // A logical condition nests conditions where a comparison holds operands; the strict reader decides by the parent's kind.
        if (shape == EsmBaselineShape.Condition && (string.Equals(member, "left", StringComparison.Ordinal) || string.Equals(member, "right", StringComparison.Ordinal)))
        {
            pending.Enqueue(EsmBaselineShape.Condition);
            return new JsonObject { ["anyOf"] = new JsonArray(Reference(child), Reference(EsmBaselineShape.Condition)) };
        }

        return EsmJsonSchemaKinds.Nested(kind, Reference(child));
    }

    static JsonObject Reference(EsmBaselineShape shape) => new() { ["$ref"] = "#/$defs/" + shape };
}
