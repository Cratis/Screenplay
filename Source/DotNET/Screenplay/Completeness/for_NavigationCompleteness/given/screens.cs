// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Completeness.for_NavigationCompleteness.given;

public class screens : Specification
{
    protected CompilationResult<ApplicationSyntax> Compilation;
    protected ImmutableArray<Diagnostic> Findings;

    protected void Compile(string screens, string module = "", string root = "")
    {
        Compilation = new ScreenplayCompiler().Compile($"{root}module M\n{string.Join('\n', module.Split('\n').Select(line => $"  {line}"))}\n  feature F\n    slice StateView View\n{string.Join('\n', screens.Split('\n').Select(line => $"      {line}"))}");
        string.Join('\n', Compilation.Diagnostics.Select(diagnostic => diagnostic.Message)).ShouldEqual(string.Empty);
        Compilation.Success.ShouldBeTrue();
    }
}
