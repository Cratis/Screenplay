// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Cratis.Screenplay.Files.for_AuthoredOrder;

public class when_holding_the_typescript_compiler_to_the_authored_order : Specification
{
    readonly List<string> _mismatches = [];
    int _count;

    void Because()
    {
        using var vectors = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root(), "Source/Screenplay/Compiler/Conformance/authored-order.json")));
        foreach (var vector in vectors.RootElement.GetProperty("cases").EnumerateArray())
        {
            _count++;
            var files = vector.GetProperty("files").EnumerateObject().ToDictionary(file => file.Name, file => file.Value.GetString()!, StringComparer.Ordinal);
            string[] roots = vector.TryGetProperty("root", out var root) ? [root.GetString()!] : [.. files.Keys.Order(StringComparer.Ordinal)];
            var compiler = new ScreenplayCompiler();
            var (documents, result) = PlayApplicationAssembly.Compile(compiler, roots, new InMemoryPlayDocumentSource(files));
            var orderingRoot = OrderingRoot.Select(roots, documents, compiler.Languages);
            var order = orderingRoot is null ? new Dictionary<string, int>() : AuthoredOrder.Record([orderingRoot], documents, compiler.Languages);
            var expectedOrder = vector.GetProperty("order").EnumerateArray().Select(scope => JsonSerializer.Serialize(scope.EnumerateArray().Select(name => name.GetString()).ToArray())).ToArray();
            var expectedDiagnostics = vector.GetProperty("diagnostics").EnumerateArray().Select(diagnostic => diagnostic.GetString()).ToArray();
            var actualDiagnostics = result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}@{diagnostic.Location.Path}:{diagnostic.Location.Line}").ToArray();
            if (!order.Keys.SequenceEqual(expectedOrder) || !actualDiagnostics.SequenceEqual(expectedDiagnostics))
            {
                _mismatches.Add($"{vector.GetProperty("name").GetString()}: order [{string.Join(", ", order.Keys)}]; diagnostics [{string.Join(", ", actualDiagnostics)}]");
            }
        }
    }

    [Fact] void should_hold_every_vector() => _count.ShouldBeGreaterThan(7);
    [Fact] void should_hold_both_compilers_to_the_same_order_and_diagnostics() => string.Join('\n', _mismatches).ShouldEqual(string.Empty);

    static string Root([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation")))
        {
            directory = directory.Parent;
        }

        return directory!.FullName;
    }
}
