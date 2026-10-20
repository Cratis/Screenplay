// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Files.for_PlayFileCompiler.when_compiling_a_folder_from_memory;

public class and_a_document_has_an_error : given.documents_in_memory
{
    ApplicationCompilation<ApplicationSyntax> _memory;
    ApplicationCompilation<ApplicationSyntax> _disk;

    void Establish() => _documents = [new("a.play", "concept Id : Uuid"), new("nested/b.play", "concept Id : String")];

    void Because()
    {
        _memory = _compiler.CompileFolder(_documents);
        _disk = FromDisk();
    }

    [Fact] void should_fail() => _memory.Result.Success.ShouldBeFalse();
    [Fact] void should_report_an_error() => _memory.Result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Error);
    [Fact] void should_locate_the_error_in_its_document() => _memory.Result.Diagnostics.Single().Location.Path.ShouldEqual("nested/b.play");
    [Fact] void should_match_the_disk_diagnostics() => _memory.Result.Diagnostics.ShouldEqual(_disk.Result.Diagnostics);
    [Fact] void should_retain_the_same_syntax_as_disk() => SyntaxJson.StructurallyEqual(_memory.Result.Value!, _disk.Result.Value!).ShouldBeTrue();
}
