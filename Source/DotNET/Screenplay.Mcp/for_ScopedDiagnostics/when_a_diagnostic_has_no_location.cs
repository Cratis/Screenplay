// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Mcp.for_ScopedDiagnostics;

public class when_a_diagnostic_has_no_location : Specification
{
    ScopedDiagnosticResult _result;
    McpSnapshot _snapshot;
    Diagnostic _diagnostic;

    void Establish()
    {
        var sources = new Dictionary<string, string> { ["application.play"] = "module M\n  feature F\n    slice StateChange Clean\n      event Added\n        value String" };
        var compiler = new McpAnalysisCompiler();
        var compilation = new PlayFileCompiler(new McpSnapshot(sources), compiler).CompileFolder(".").Result;
        _diagnostic = Diagnostic.Error("PLAY0001", "Application-wide failure", new(0, 0));
        _snapshot = new(sources, string.Empty, compiler.Languages, compiler, new CompilationResult<ApplicationSyntax>(compilation.Value, [.. compilation.Diagnostics, _diagnostic]));
    }

    void Because() => _result = ScopedDiagnostics.Select(_snapshot, "M.F.Clean")!;

    [Fact] void should_include_the_unlocated_application_diagnostic() => _result.Diagnostics.ShouldContain(_diagnostic);
}
