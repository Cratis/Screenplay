// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json;
using Cratis.Screenplay.Files;

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph.given;

public class a_conformance_suite : Specification
{
    protected JsonElement[] _vectors;

    void Establish()
    {
        using var vectors = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root(), "Source/Screenplay/Compiler/Conformance/dependency-graph.json")));
        _vectors = [.. vectors.RootElement.GetProperty("cases").EnumerateArray().Select(vector => vector.Clone())];
    }

    private protected static DependencyGraph Graph(JsonElement vector, bool reverse = false)
    {
        var files = vector.GetProperty("files").EnumerateObject().ToDictionary(file => file.Name, file => file.Value.GetString()!, StringComparer.Ordinal);
        var compiler = new ScreenplayCompiler();
        PlayApplicationAssembly.Compile(compiler, reverse ? files.Keys.Reverse() : files.Keys, new InMemoryPlayDocumentSource(files), compiler.Languages, out var timeline);

        return DependencyGraph.For(timeline);
    }

    internal static string Root([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation"))) directory = directory.Parent;

        return directory!.FullName;
    }
}
