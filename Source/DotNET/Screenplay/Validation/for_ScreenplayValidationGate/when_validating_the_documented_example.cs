// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Completeness;

namespace Cratis.Screenplay.Validation.for_ScreenplayValidationGate;

public class when_validating_the_documented_example : given.a_gate_file
{
    JsonDocument _schema;
    JsonDocument _example;
    ValidationGateReadResult _read;
    bool _valid;

    void Establish()
    {
        _schema = JsonDocument.Parse(File.ReadAllText(Path.Combine(Repository, "Documentation", "screenplay", "screenplay-v1.schema.json")));
        var documentation = File.ReadAllText(Path.Combine(Repository, "Documentation", "screenplay", "tool.md"));
        var section = documentation.Split("## Declare the validation gate once", StringSplitOptions.None)[1];
        var example = section.Split("```json", StringSplitOptions.None)[1].Split("```", StringSplitOptions.None)[0].Trim();
        _example = JsonDocument.Parse(example);
        File.WriteAllText(GatePath, example);
    }

    void Because()
    {
        _valid = Matches(_schema.RootElement, _example.RootElement);
        _read = ScreenplayValidationGate.Read(Root);
    }

    void Destroy()
    {
        _schema.Dispose();
        _example.Dispose();
    }

    [Fact] void should_validate_the_example_against_every_schema_constraint() => _valid.ShouldBeTrue();
    [Fact] void should_read_the_example_without_refusal() => _read.Failure.ShouldBeNull();
    [Fact] void should_read_the_documented_checks() => _read.Declaration.Checks.Value.ToArray().ShouldEqual(new[] { "field-origins", "query-keys", "navigation" });
    [Fact] void should_keep_the_schema_enum_equal_to_the_parser_set() => _schema.RootElement.GetProperty("properties").GetProperty("validation").GetProperty("properties").GetProperty("checks").GetProperty("items").GetProperty("enum").EnumerateArray().Select(item => item.GetString()).ShouldEqual(CompletenessChecks.AcceptedNames);
    [Fact] void should_forbid_unknown_top_level_members() => _schema.RootElement.GetProperty("additionalProperties").GetBoolean().ShouldBeFalse();
    [Fact] void should_forbid_unknown_validation_members() => _schema.RootElement.GetProperty("properties").GetProperty("validation").GetProperty("additionalProperties").GetBoolean().ShouldBeFalse();

    static bool Matches(JsonElement schema, JsonElement value)
    {
        string[] supported = ["$schema", "$id", "title", "type", "additionalProperties", "properties", "items", "enum", "pattern"];
        if (!schema.EnumerateObject().All(property => supported.Contains(property.Name, StringComparer.Ordinal))) return false;
        if (schema.TryGetProperty("enum", out var choices) && !choices.EnumerateArray().Any(choice => choice.GetRawText() == value.GetRawText())) return false;
        return schema.GetProperty("type").GetString() switch
        {
            "object" => MatchObject(schema, value),
            "array" => MatchArray(schema, value),
            "boolean" => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
            "string" => MatchString(schema, value),
            _ => false
        };
    }

    static bool MatchObject(JsonElement schema, JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object) return false;
        var properties = schema.GetProperty("properties");
        return value.EnumerateObject().All(property => properties.TryGetProperty(property.Name, out var child) ? Matches(child, property.Value) : schema.GetProperty("additionalProperties").GetBoolean());
    }

    static bool MatchArray(JsonElement schema, JsonElement value) => value.ValueKind == JsonValueKind.Array && value.EnumerateArray().All(item => Matches(schema.GetProperty("items"), item));

    static bool MatchString(JsonElement schema, JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String) return false;
        return !schema.TryGetProperty("pattern", out var pattern) || Regex.IsMatch(value.GetString(), pattern.GetString(), RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
    }
}
