// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json;
using Cratis.Screenplay.Files;

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph;

public class when_holding_the_typescript_compiler_to_the_dependency_graph
{
    [Fact]
    public void should_hold_shared_vectors_and_be_independent_of_document_arrival_order()
    {
        using var vectors = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root(), "Source/Screenplay/Compiler/Conformance/dependency-graph.json")));
        foreach (var vector in vectors.RootElement.GetProperty("cases").EnumerateArray())
        {
            var files = vector.GetProperty("files").EnumerateObject().ToDictionary(file => file.Name, file => file.Value.GetString()!, StringComparer.Ordinal);
            string? first = null;
            foreach (var reverse in new[] { false, true })
            {
                var compiler = new ScreenplayCompiler();
                PlayApplicationAssembly.Compile(compiler, reverse ? files.Keys.Reverse() : files.Keys, new InMemoryPlayDocumentSource(files), compiler.Languages, out var timeline);
                var graph = DependencyGraph.For(timeline);
                var edges = graph.Edges.Select(edge => $"{edge.Consumer.Address}|{edge.Producer.Address}|{edge.Kind}|{string.Join(',', edge.Evidence.Select(item => item.Role + ":" + item.Name))}").ToArray();
                var implied = graph.Implied("feature", "feature").Select(edge => $"{edge.Source.Address}|{edge.Target.Address}|{edge.SliceEdges}|{edge.References}").ToArray();
                var cycles = graph.Cycles("feature").Select(group => group.Members.Select(node => node.Address).ToArray()).ToArray();
                var order = graph.SuggestedOrder().Slices.Select(node => node.Address).ToArray();
                edges.ShouldEqual(vector.GetProperty("edges").EnumerateArray().Select(item => item.GetString()).ToArray());
                implied.ShouldEqual(vector.GetProperty("implied").EnumerateArray().Select(item => item.GetString()).ToArray());
                JsonSerializer.Serialize(cycles).ShouldEqual(vector.GetProperty("cycles").GetRawText().Replace(" ", string.Empty));
                order.ShouldEqual(vector.GetProperty("order").EnumerateArray().Select(item => item.GetString()).ToArray());
                var output = JsonSerializer.Serialize(new { graph.Edges, Implied = graph.Implied("feature", "feature"), Cycles = graph.Cycles("feature"), Order = graph.SuggestedOrder(), graph.Unresolved });
                if (first is not null) output.ShouldEqual(first);
                first = output;
            }
        }
    }

    internal static string Root([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation"))) directory = directory.Parent;

        return directory!.FullName;
    }
}
