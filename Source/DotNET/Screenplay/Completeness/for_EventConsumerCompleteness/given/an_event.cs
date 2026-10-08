// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Completeness.for_EventConsumerCompleteness.given;

public class an_event : Specification
{
    protected CompilationResult<ApplicationSyntax> Compilation;
    protected ImmutableArray<Diagnostic> Findings;

    protected void Compile(string body, string declarations = "")
    {
        Compilation = new ScreenplayCompiler().Compile($"{declarations}module M\n  feature F\n    slice StateView View\n      event Changed\n        id Uuid\n{string.Join('\n', body.Split('\n').Select(line => $"      {line}"))}");
        string.Join('\n', Compilation.Diagnostics.Select(diagnostic => diagnostic.Message)).ShouldEqual(string.Empty);
        Compilation.Success.ShouldBeTrue();
    }
}
