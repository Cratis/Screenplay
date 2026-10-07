// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph.given;

public class a_model : Specification
{
    protected const string Producer = "module M\n  feature A\n    slice StateChange Producer\n      event E\n      command C\n      query Q => R\n      readmodel R\n      projection R\n        from E\n      screen S\n";
    protected string _source;
    private protected DependencyGraph _graph;

    private protected static DependencyGraph Graph(string source) => DependencyGraph.For(new ScreenplayCompiler().Parse(source, "model.play").Value!);
}
