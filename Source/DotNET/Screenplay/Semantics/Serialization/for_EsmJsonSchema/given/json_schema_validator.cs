// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization.for_EsmJsonSchema.given;

/// <summary>
/// Validates JSON against the JSON Schema subset the ESM schemas are generated in: $ref into $defs, type, const,
/// properties, additionalProperties false, required, items and anyOf. Any other keyword fails, so the schemas
/// cannot grow a keyword this validator silently ignores.
/// </summary>
/// <param name="schema">The root schema.</param>
public sealed class json_schema_validator(JsonElement schema)
{
    static readonly HashSet<string> _keywords = ["$schema", "$id", "title", "$defs", "$ref", "type", "const", "properties", "additionalProperties", "required", "items", "anyOf"];

    /// <summary>
    /// Validates a document and returns the paths that do not conform.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <returns>Errors as JSON paths with a reason; empty when the document conforms.</returns>
    public IReadOnlyList<string> Validate(JsonElement document)
    {
        var errors = new List<string>();
        Check(schema, document, "$", errors);
        return errors;
    }

    void Check(JsonElement node, JsonElement value, string path, List<string> errors)
    {
        foreach (var keyword in node.EnumerateObject().Where(keyword => !_keywords.Contains(keyword.Name)))
        {
            errors.Add($"{path}: unsupported schema keyword '{keyword.Name}'");
        }

        if (node.TryGetProperty("$ref", out var reference))
        {
            Check(schema.GetProperty("$defs").GetProperty(reference.GetString()!["#/$defs/".Length..]), value, path, errors);
        }
        if (node.TryGetProperty("anyOf", out var choices) && !choices.EnumerateArray().Any(choice => Errors(choice, value, path).Count == 0))
        {
            errors.Add($"{path}: matches no alternative");
        }
        if (node.TryGetProperty("const", out var constant) && !JsonElement.DeepEquals(constant, value))
        {
            errors.Add($"{path}: is not {constant.GetRawText()}");
        }
        if (node.TryGetProperty("type", out var type) && !Types(type).Any(name => Is(name, value)))
        {
            errors.Add($"{path}: is {value.ValueKind}, not {type.GetRawText()}");
            return;
        }

        if (value.ValueKind == JsonValueKind.Object) Object(node, value, path, errors);
        if (value.ValueKind == JsonValueKind.Array && node.TryGetProperty("items", out var items))
        {
            var index = 0;
            foreach (var item in value.EnumerateArray()) Check(items, item, $"{path}[{index++}]", errors);
        }
    }

    void Object(JsonElement node, JsonElement value, string path, List<string> errors)
    {
        var properties = node.TryGetProperty("properties", out var declared) ? declared : default;
        foreach (var member in value.EnumerateObject())
        {
            if (properties.ValueKind == JsonValueKind.Object && properties.TryGetProperty(member.Name, out var memberSchema))
            {
                Check(memberSchema, member.Value, $"{path}.{member.Name}", errors);
            }
            else if (node.TryGetProperty("additionalProperties", out var additional) && additional.ValueKind == JsonValueKind.False)
            {
                errors.Add($"{path}: unknown member '{member.Name}'");
            }
        }

        if (!node.TryGetProperty("required", out var required)) return;
        foreach (var name in required.EnumerateArray().Select(name => name.GetString()!).Where(name => !value.TryGetProperty(name, out _)))
        {
            errors.Add($"{path}: missing required member '{name}'");
        }
    }

    List<string> Errors(JsonElement node, JsonElement value, string path)
    {
        var errors = new List<string>();
        Check(node, value, path, errors);
        return errors;
    }

    static IEnumerable<string> Types(JsonElement type) => type.ValueKind == JsonValueKind.Array ? type.EnumerateArray().Select(name => name.GetString()!) : [type.GetString()!];

    static bool Is(string type, JsonElement value) => type switch
    {
        "object" => value.ValueKind == JsonValueKind.Object,
        "array" => value.ValueKind == JsonValueKind.Array,
        "string" => value.ValueKind == JsonValueKind.String,
        "boolean" => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
        "null" => value.ValueKind == JsonValueKind.Null,
        "number" => value.ValueKind == JsonValueKind.Number,
        "integer" => value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out _),
        _ => throw new ArgumentException($"Unknown JSON Schema type '{type}'.", nameof(type))
    };
}
#endif
