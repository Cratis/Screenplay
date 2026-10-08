// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Completeness.for_QueryKeyCompleteness.given;

public class a_query : Specification
{
    protected CompilationResult<ApplicationSyntax> Compilation;
    protected ImmutableArray<Diagnostic> Findings;

    protected void Compile(string query, string key = "key sourceId", string declarations = "", string extraQueries = "", string properties = "id Uuid\nname String")
    {
        var source = $"{declarations}module M\n  feature F\n    slice StateView View\n      event Changed\n        sourceId Uuid\n        name String\n      readmodel R\n{string.Join('\n', properties.Split('\n').Select(line => $"        {line}"))}\n      projection P => R\n        from Changed\n{string.Join('\n', key.Split('\n').Select(line => $"          {line}"))}\n          id = sourceId\n{string.Join('\n', $"{query}\n{extraQueries}".Split('\n').Select(line => $"      {line}"))}";
        Compilation = new ScreenplayCompiler().Compile(source);
        string.Join('\n', Compilation.Diagnostics.Select(diagnostic => diagnostic.Message)).ShouldEqual(string.Empty);
        Compilation.Success.ShouldBeTrue();
    }
}
