// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Completeness.for_FieldOriginCompleteness.given;

public class a_view : Specification
{
    protected CompilationResult<ApplicationSyntax> Compilation;
    protected ImmutableArray<Diagnostic> Findings;

    protected void Compile(string source, string declarations = "")
    {
        Compilation = new ScreenplayCompiler().Compile($"{declarations}module M\n  feature F\n{string.Join('\n', source.Split('\n').Select(line => $"    {line}"))}");
        string.Join('\n', Compilation.Diagnostics.Select(diagnostic => diagnostic.Message)).ShouldEqual(string.Empty);
        Compilation.Success.ShouldBeTrue();
    }
}
