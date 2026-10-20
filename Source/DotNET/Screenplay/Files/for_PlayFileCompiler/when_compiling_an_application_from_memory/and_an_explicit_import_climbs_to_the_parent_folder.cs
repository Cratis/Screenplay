// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Files.for_PlayFileCompiler.when_compiling_an_application_from_memory;

public class and_an_explicit_import_climbs_to_the_parent_folder : when_compiling_a_folder_from_memory.given.documents_in_memory
{
    ApplicationCompilation<ApplicationSyntax> _memory;
    ApplicationCompilation<ApplicationSyntax> _disk;

    void Establish() => _documents =
    [
        new("apps/application.play", "import \"../shared.play\""),
        new("shared.play", "concept Id : Uuid"),
        new("unused.play", "concept Id : String")
    ];

    void Because()
    {
        _memory = _compiler.CompileApplication("apps/application.play", _documents);
        _disk = FromDisk("apps/application.play");
    }

    [Fact] void should_have_no_diagnostics() => _memory.Result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_succeed_on_disk() => _disk.Result.Success.ShouldBeTrue();
    [Fact] void should_match_the_disk_syntax() => SyntaxJson.StructurallyEqual(_memory.Result.Value!, _disk.Result.Value!).ShouldBeTrue();
    [Fact] void should_match_the_disk_diagnostics() => _memory.Result.Diagnostics.ShouldEqual(_disk.Result.Diagnostics);
    [Fact] void should_read_the_explicit_parent_document() => _memory.Sources.Select(source => source.File.Path).ShouldContainOnly("apps/application.play", "shared.play");
    [Fact] void should_match_the_disk_relative_paths() => _memory.Sources.Select(source => source.File.RelativePath).ShouldEqual(_disk.Sources.Select(source => source.File.RelativePath));
}
