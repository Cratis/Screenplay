// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFileCompiler.when_compiling_a_folder;

public class and_two_files_declare_a_domain : given.a_folder_of_play_files
{
    ApplicationCompilation<ApplicationSyntax> _compilation;
    Diagnostic _diagnostic;

    void Establish()
    {
        Write("first.play", "domain Sales");
        Write("second.play", "domain Shipping");
    }

    void Because()
    {
        _compilation = _compiler.CompileFolder(_root.FullName);
        _diagnostic = _compilation.Result.Diagnostics.Single();
    }

    [Fact] void should_not_succeed() => _compilation.Result.Success.ShouldBeFalse();
    [Fact] void should_report_an_error() => _diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Error);
    [Fact] void should_name_the_file_that_declared_it_first() => _diagnostic.Message.ShouldEqual("The folder already declares a domain in 'first.play' - a folder compiles to one application, which can have at most one");
    [Fact] void should_point_at_the_offending_file() => _diagnostic.Location.Path.ShouldEqual("second.play");
    [Fact] void should_keep_the_first_domain() => _compilation.Result.Value!.Domain!.Name.ShouldEqual("Sales");
}
