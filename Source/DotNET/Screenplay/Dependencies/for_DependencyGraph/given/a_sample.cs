// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Files;

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph.given;

public class a_sample : Specification
{
    private protected AuthoredTimeline _timeline;
    private protected DependencyGraph _graph;

    protected void Load(string sample)
    {
        var folder = Path.Combine(a_conformance_suite.Root(), "Samples", sample);
        var files = Directory.GetFiles(folder, "*.play", SearchOption.AllDirectories).Order(StringComparer.Ordinal)
            .ToDictionary(path => Path.GetRelativePath(folder, path).Replace('\\', '/'), File.ReadAllText, StringComparer.Ordinal);
        var compiler = new ScreenplayCompiler();
        PlayApplicationAssembly.Compile(compiler, files.Keys, new InMemoryPlayDocumentSource(files), compiler.Languages, out _timeline);
    }
}
