// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

// The TypeScript compiler (Source/Screenplay/Compiler) writes the syntax it reads for each document of a
// shared corpus as golden files, and holds itself to them. This holds the C# compiler to the same files:
// for every member the TypeScript compiler models, the C# syntax has to say the same. A member only the C#
// compiler models is not compared - the TypeScript compiler leaves it out rather than claiming it empty.
public class when_holding_the_typescript_compiler_to_it : Specification
{
    readonly List<string> _mismatches = [];
    List<(string Name, string Path)> _documents;
    int _compared;

    void Establish() => _documents = [.. Documents()];

    void Because()
    {
        foreach (var (name, path) in _documents)
        {
            var parsed = new ScreenplayCompiler().Parse(File.ReadAllText(Path.Combine(Root(), path)));
            using var golden = JsonDocument.Parse(File.ReadAllText(Path.Combine(Conformance(), $"{name}.syntax.json")));
            Compare(golden.RootElement, SyntaxJson.Serialize(parsed.Value!), $"{name}: $");
        }
    }

    // Without these two the comparison could pass by reading nothing - a moved manifest or an emptied golden
    // file would otherwise turn it into a check of nothing against nothing.
    [Fact] void should_hold_documents() => _documents.Count.ShouldBeGreaterThan(5);
    [Fact] void should_compare_members() => _compared.ShouldBeGreaterThan(5000);

    [Fact] void should_agree_on_every_member_the_typescript_compiler_models() => Report().ShouldEqual(string.Empty);

    static string Root([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation")))
        {
            directory = directory.Parent;
        }

        return directory!.FullName;
    }

    static string Conformance() => Path.Combine(Root(), "Source", "Screenplay", "Compiler", "Conformance");

    static IEnumerable<(string Name, string Path)> Documents()
    {
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(Conformance(), "manifest.json")));
        return [.. manifest.RootElement.GetProperty("documents").EnumerateArray()
            .Select(document => (document.GetProperty("name").GetString()!, document.GetProperty("path").GetString()!))];
    }

    static bool SameScalar(JsonElement golden, JsonElement actual) => golden.ValueKind switch
    {
        JsonValueKind.String => actual.ValueKind == JsonValueKind.String && golden.GetString() == actual.GetString(),
        JsonValueKind.Number => actual.ValueKind == JsonValueKind.Number && golden.GetDouble() == actual.GetDouble(),
        _ => golden.ValueKind == actual.ValueKind
    };

    void Compare(JsonElement golden, JsonElement actual, string path)
    {
        switch (golden.ValueKind)
        {
            case JsonValueKind.Object when actual.ValueKind == JsonValueKind.Object:
                foreach (var member in golden.EnumerateObject())
                {
                    _compared++;
                    if (actual.TryGetProperty(member.Name, out var value))
                    {
                        Compare(member.Value, value, $"{path}.{member.Name}");
                    }
                    else
                    {
                        _mismatches.Add($"{path}.{member.Name}: the C# syntax has no such member");
                    }
                }

                break;

            case JsonValueKind.Array when actual.ValueKind == JsonValueKind.Array && golden.GetArrayLength() == actual.GetArrayLength():
                var index = 0;
                foreach (var (expected, item) in golden.EnumerateArray().Zip(actual.EnumerateArray()))
                {
                    Compare(expected, item, $"{path}[{index++}]");
                }

                break;

            case JsonValueKind.Object or JsonValueKind.Array:
                _mismatches.Add($"{path}: TypeScript has {Describe(golden)}, C# has {Describe(actual)}");
                break;

            default:
                if (!SameScalar(golden, actual))
                {
                    _mismatches.Add($"{path}: TypeScript has {golden.GetRawText()}, C# has {actual.GetRawText()}");
                }

                break;
        }
    }

    static string Describe(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Array => $"{element.GetArrayLength()} item(s)",
        JsonValueKind.Object when element.TryGetProperty("kind", out var kind) => $"a {kind.GetString()}",
        _ => element.GetRawText()
    };

    string Report() =>
        _mismatches.Count == 0
            ? string.Empty
            : $"The TypeScript compiler reads {_mismatches.Count} member(s) differently from the C# compiler:{Environment.NewLine}" +
              string.Join(Environment.NewLine, _mismatches.Take(50));
}
