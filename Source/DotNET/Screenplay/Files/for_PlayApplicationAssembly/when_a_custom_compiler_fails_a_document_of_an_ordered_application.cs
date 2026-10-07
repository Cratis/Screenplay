// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayApplicationAssembly;

public class when_a_custom_compiler_fails_a_document_of_an_ordered_application : Specification
{
    static readonly Diagnostic _failure = new(DiagnosticSeverity.Error, "PLAY0001", "The document could not be read.", new SourceLocation(1, 1, "Ordering/Ordering.play"));

    readonly Dictionary<string, string> _documents = new(StringComparer.Ordinal)
    {
        ["application.play"] = "import \"Ordering/Ordering.play\"",
        ["Ordering/Ordering.play"] = "module Ordering\n  feature Orders\n    slice StateView Orders"
    };

    IScreenplayCompiler _compiler;
    Exception _error;
    CompilationResult<ApplicationSyntax> _result;

    void Establish()
    {
        var compiler = new ScreenplayCompiler();
        _compiler = Substitute.For<IScreenplayCompiler>();
        _compiler.Parse(Arg.Any<string>(), Arg.Any<string?>()).Returns(call => call.ArgAt<string?>(1) == "Ordering/Ordering.play"
            ? CompilationResult<ApplicationSyntax>.Failed([_failure])
            : compiler.Parse(call.ArgAt<string>(0), call.ArgAt<string?>(1)));
        _compiler.Parse(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<PlayPlacement>()).Returns(call => call.ArgAt<string?>(1) == "Ordering/Ordering.play"
            ? CompilationResult<ApplicationSyntax>.Failed([_failure])
            : compiler.Parse(call.ArgAt<string>(0), call.ArgAt<string?>(1), call.ArgAt<PlayPlacement>(2)));
    }

    void Because() => _error = Catch.Exception(() => (_, _result) = PlayApplicationAssembly.Compile(_compiler, ["application.play"], new InMemoryPlayDocumentSource(_documents)));

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_report_the_compiler_failure() => _result.Diagnostics.ShouldContain(_failure);
}
