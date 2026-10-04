// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxSchema;

public class when_holding_exact_writer_collection_contracts
{
    [Fact]
    void should_hold_every_collection_and_item_kind_to_the_native_schema_without_bundling_the_full_reader()
    {
        var definitions = SyntaxSchema.For(nameof(ApplicationSyntax)).GetProperty("$defs");
        var nodes = definitions.EnumerateObject().Where(definition => definition.Value.TryGetProperty("properties", out var properties) && properties.TryGetProperty("kind", out _)).ToArray();
        var kinds = nodes.Select(node => node.Name).ToList();
        var collections = new List<object[]>();
        foreach (var node in nodes)
        {
            var contracts = new List<object[]>();
            foreach (var member in node.Value.GetProperty("properties").EnumerateObject())
            {
                var alternatives = member.Value.TryGetProperty("anyOf", out var choices) ? choices.EnumerateArray().ToArray() : [member.Value];
                var array = alternatives.FirstOrDefault(alternative => HasType(alternative, "array"));
                if (array.ValueKind == JsonValueKind.Undefined) continue;
                var nullable = alternatives.Any(alternative => HasType(alternative, "null"));
                var items = array.GetProperty("items");
                var allowed = items.TryGetProperty("oneOf", out var itemChoices) ? itemChoices.EnumerateArray().ToArray() : [items];
                var indices = allowed.Select(item =>
                {
                    var kind = item.TryGetProperty("$ref", out var reference) ? reference.GetString()!["#/$defs/".Length..] : $"type:{item.GetProperty("type").GetString()}";
                    var index = kinds.IndexOf(kind);
                    if (index >= 0) return index;
                    kinds.Add(kind);
                    return kinds.Count - 1;
                }).ToArray();
                contracts.Add([member.Name, nullable, indices]);
            }
            collections.Add([.. contracts]);
        }
        while (collections.Count < kinds.Count) collections.Add([]);
        var json = JsonSerializer.Serialize(new { kinds, collections });
        var rendered = "// Copyright (c) Cratis. All rights reserved.\n" +
            "// Licensed under the MIT license. See LICENSE file in the project root for full license information.\n\n" +
            "// Generated from C# SyntaxSchema; collection contracts and kind indices are native-owned.\n" +
            $"export const syntaxCollections = {json} as const;\n";
        var path = Path.Combine(Root(), "Source", "Screenplay", "Compiler", "Syntax", "SyntaxCollections.ts");
        var regenerating = Environment.GetEnvironmentVariable("SCREENPLAY_REGENERATE_COLLECTION_CONTRACTS") == "1";
        if (regenerating) File.WriteAllText(path, rendered);
        Assert.False(regenerating, "Review the generated collection contracts and rerun without SCREENPLAY_REGENERATE_COLLECTION_CONTRACTS.");
        File.ReadAllText(path).ShouldEqual(rendered);
    }

    static bool HasType(JsonElement schema, string type) => schema.TryGetProperty("type", out var value) && value.ValueKind == JsonValueKind.String && value.GetString() == type;

    static string Root([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation"))) directory = directory.Parent;

        return directory!.FullName;
    }
}
