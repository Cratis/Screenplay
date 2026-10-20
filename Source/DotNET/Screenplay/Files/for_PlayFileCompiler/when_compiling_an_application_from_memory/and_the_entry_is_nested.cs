// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Files.for_PlayFileCompiler.when_compiling_an_application_from_memory;

public class and_the_entry_is_nested : when_compiling_a_folder_from_memory.given.documents_in_memory
{
    ApplicationCompilation<ApplicationSyntax> _memory;
    ApplicationCompilation<ApplicationSyntax> _disk;

    void Establish() => _documents =
    [
        new("apps/application.play", "import \"../z-sources.play\"\nmodule Banking\n  feature Accounts\n    import \"commands/*.play\""),
        new("apps/commands/record.play", "slice StateChange Record\n  command Record\n    stream Account.Ledger\n      streamId\n        account = \"a\"\n        period = \"2026-10\""),
        new("z-sources.play", EventSource),
        new("unused.play", "concept Duplicate : String\nconcept Duplicate : Uuid")
    ];

    void Because()
    {
        _memory = _compiler.CompileApplication("apps/application.play", _documents);
        _disk = FromDisk("apps/application.play");
    }

    [Fact] void should_succeed() => _memory.Result.Success.ShouldBeTrue();
    [Fact] void should_resolve_imported_streams() => _memory.Result.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single().Stream!.Stream.ShouldEqual("Ledger");
    [Fact] void should_match_the_disk_syntax() => SyntaxJson.StructurallyEqual(_memory.Result.Value!, _disk.Result.Value!).ShouldBeTrue();
    [Fact] void should_match_the_disk_diagnostics() => _memory.Result.Diagnostics.ShouldEqual(_disk.Result.Diagnostics);
    [Fact] void should_exclude_unreachable_documents() => _memory.Sources.Count().ShouldEqual(3);
    [Fact] void should_rebase_relative_paths_to_the_entry_folder() => _memory.Sources.Select(source => source.File.RelativePath).ShouldEqual(_disk.Sources.Select(source => source.File.RelativePath));
    [Fact] void should_retain_document_set_paths() => _memory.Sources.Select(source => source.File.Path).ShouldContainOnly("apps/application.play", "apps/commands/record.play", "z-sources.play");
}
