// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Completeness.for_DataBindingCompleteness.given;

public class a_screen : Specification
{
    protected CompilationResult<ApplicationSyntax> Compilation;
    protected ImmutableArray<Diagnostic> Findings;

    protected void Compile(string directives)
    {
        var source = """
            module M
              feature F
                slice StateView View
                  readmodel R
                    value String
                  readmodel Other
                    value String
                  query Q => R[]
                  query Q2 => R[]
                  query One => R
                  query Different => Other[]
                  screen S
            """ + "\n" + string.Join('\n', directives.Split('\n').Select(line => "        " + line));
        Compilation = new ScreenplayCompiler().Compile(source);
        Compilation.Success.ShouldBeTrue();
    }
}
