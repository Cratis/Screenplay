// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Screenplay.Semantics.Serialization;

/// <summary>
/// Translates the kind codes of <see cref="EsmSchemaMembers"/> into JSON Schema fragments.
/// </summary>
internal static class EsmJsonSchemaKinds
{
    /// <summary>
    /// Gets the schema of a member that holds a plain JSON value.
    /// </summary>
    /// <param name="kind">The kind code.</param>
    /// <returns>The schema fragment.</returns>
    /// <exception cref="InvalidSemanticContract">The code is not a value kind.</exception>
    internal static JsonObject Atom(string kind) => kind switch
    {
        "s" => Type("string"),
        "s?" => Types("string", "null"),
        "b" => Type("boolean"),
        "b?" => Types("boolean", "null"),
        "i" => Type("integer"),
        "i?" => Types("integer", "null"),
        "s[]" => new() { ["type"] = "array", ["items"] = Type("string") },
        "i[]" => new() { ["type"] = "array", ["items"] = Type("integer") },
        "scalar" => Types("string", "number", "boolean"),
        "path" => new() { ["anyOf"] = new JsonArray(Type("string"), new JsonObject { ["type"] = "array", ["items"] = Type("string") }) },
        "null" => Type("null"),
        _ => throw new InvalidSemanticContract($"ESM member kind '{kind}' does not describe a plain value.")
    };

    /// <summary>
    /// Gets the schema of a member that holds one or more nested shapes.
    /// </summary>
    /// <param name="kind">The kind code.</param>
    /// <param name="reference">The reference to the nested shape's definition.</param>
    /// <returns>The schema fragment.</returns>
    /// <exception cref="InvalidSemanticContract">The code is not a nesting kind.</exception>
    internal static JsonObject Nested(string kind, JsonObject reference) => kind switch
    {
        "1" => reference,
        "1?" => new() { ["anyOf"] = new JsonArray(reference, Type("null")) },
        "[]" => new() { ["type"] = "array", ["items"] = reference },
        _ => throw new InvalidSemanticContract($"ESM member kind '{kind}' does not describe a nested shape.")
    };

    static JsonObject Type(string type) => new() { ["type"] = type };

    static JsonObject Types(params string[] types) => new() { ["type"] = new JsonArray([.. types.Select(type => (JsonNode?)JsonValue.Create(type))]) };
}
