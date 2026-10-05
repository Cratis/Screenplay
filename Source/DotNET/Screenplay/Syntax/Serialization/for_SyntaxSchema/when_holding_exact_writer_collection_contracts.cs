// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxSchema;

public class when_holding_exact_writer_collection_contracts
{
    [Fact]
    void should_hold_every_member_contract_to_the_native_schema_without_bundling_the_full_reader()
    {
        var definitions = SyntaxSchema.For(nameof(ApplicationSyntax)).GetProperty("$defs");
        var nodes = definitions.EnumerateObject().Where(definition => definition.Value.TryGetProperty("properties", out var properties) && properties.TryGetProperty("kind", out _)).ToArray();
        var kinds = nodes.Select(node => node.Name).ToList();
        var names = new List<string>();
        var rules = new List<object[]>();
        var indices = new Dictionary<string, int>();
        var contracts = nodes.Select(node => ObjectRule(node.Value, true)).ToArray();
        var json = JsonSerializer.Serialize(new { kinds, names, rules, contracts });
        var rendered = "// Copyright (c) Cratis. All rights reserved.\n" +
            "// Licensed under the MIT license. See LICENSE file in the project root for full license information.\n\n" +
            "// Generated from C# SyntaxSchema; all typed member contracts are native-owned.\n" +
            $"export const syntaxCollections = {json} as const;\n";
        var path = Path.Combine(Root(), "Source", "Screenplay", "Compiler", "Syntax", "SyntaxCollections.ts");
        var regenerating = Environment.GetEnvironmentVariable("SCREENPLAY_REGENERATE_COLLECTION_CONTRACTS") == "1";
        if (regenerating) File.WriteAllText(path, rendered);
        Assert.False(regenerating, "Review the generated member contracts and rerun without SCREENPLAY_REGENERATE_COLLECTION_CONTRACTS.");
        File.ReadAllText(path).ShouldEqual(rendered);

        int Intern(object[] rule)
        {
            var key = JsonSerializer.Serialize(rule);
            if (indices.TryGetValue(key, out var index)) return index;
            index = rules.Count;
            indices.Add(key, index);
            rules.Add(rule);

            return index;
        }

        int ObjectRule(JsonElement schema, bool node = false)
        {
            var required = schema.TryGetProperty("required", out var requiredNames) ? requiredNames.EnumerateArray().Select(name => name.GetString()).ToHashSet() : [];
            var members = new List<object[]>();
            foreach (var member in schema.GetProperty("properties").EnumerateObject())
            {
                if (node && member.Name == "kind") continue;
                var index = names.IndexOf(member.Name);
                if (index < 0)
                {
                    index = names.Count;
                    names.Add(member.Name);
                }
                members.Add([index, Rule(member.Value), required.Contains(member.Name)]);
            }

            return Intern(["object", members]);
        }

        int Rule(JsonElement schema)
        {
            if (schema.TryGetProperty("$ref", out var reference)) return Intern(["ref", kinds.IndexOf(reference.GetString()!["#/$defs/".Length..])]);
            if (schema.TryGetProperty("anyOf", out var any) || schema.TryGetProperty("oneOf", out any))
            {
                var alternatives = any.EnumerateArray().ToArray();
                if (alternatives.All(alternative => alternative.TryGetProperty("$ref", out _))) return Intern(["ref", .. alternatives.Select(alternative => (object)kinds.IndexOf(alternative.GetProperty("$ref").GetString()!["#/$defs/".Length..]))]);

                return Intern([schema.TryGetProperty("oneOf", out _) ? "one" : "union", .. alternatives.Select(alternative => (object)Rule(alternative))]);
            }
            if (schema.TryGetProperty("const", out var constant)) return Intern(["const", constant.Clone()]);
            if (schema.TryGetProperty("enum", out var values)) return Intern(["enum", .. values.EnumerateArray().Select(value => (object)value.Clone())]);
            if (!schema.TryGetProperty("type", out var type)) return Intern(["any"]);
            if (type.ValueKind == JsonValueKind.Array) return Intern(["union", .. type.EnumerateArray().Select(value => (object)Intern([value.GetString()!]))]);
            var name = type.GetString()!;
            if (name == "object") return ObjectRule(schema);
            if (name == "array") return Intern([name, Rule(schema.GetProperty("items"))]);
            if (schema.TryGetProperty("pattern", out var pattern)) return Intern([name, pattern.GetString()!]);
            if (schema.TryGetProperty("minimum", out var minimum)) return Intern([name, minimum.Clone(), schema.GetProperty("maximum").Clone()]);

            return Intern([name]);
        }
    }

    static string Root([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation"))) directory = directory.Parent;

        return directory!.FullName;
    }
}
