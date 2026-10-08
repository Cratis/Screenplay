// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Completeness.for_InputSurfaceCompleteness.given;

public class a_model : Specification
{
    protected CompilationResult<ApplicationSyntax> Compilation;
    protected ImmutableArray<Diagnostic> Findings;

    protected void Compile(string source)
    {
        Compilation = new ScreenplayCompiler().Compile(source);
        string.Join('\n', Compilation.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Select(diagnostic => diagnostic.Message)).ShouldEqual(string.Empty);
        Compilation.Success.ShouldBeTrue();
    }
}
