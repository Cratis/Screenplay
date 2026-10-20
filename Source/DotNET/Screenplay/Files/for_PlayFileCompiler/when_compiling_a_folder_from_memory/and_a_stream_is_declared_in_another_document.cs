// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Files.for_PlayFileCompiler.when_compiling_a_folder_from_memory;

public class and_a_stream_is_declared_in_another_document : given.documents_in_memory
{
    ApplicationCompilation<ApplicationSyntax> _memory;
    ApplicationCompilation<ApplicationSyntax> _disk;
    CommandStreamSyntax _stream;

    void Because()
    {
        _memory = _compiler.CompileFolder(_documents);
        _disk = FromDisk();
        _stream = _memory.Result.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single().Stream!;
    }

    [Fact] void should_succeed() => _memory.Result.Success.ShouldBeTrue();
    [Fact] void should_resolve_cross_file_references_without_diagnostics() => _memory.Result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_route_to_the_declared_event_source() => _stream.EventSource.ShouldEqual("Account");
    [Fact] void should_route_to_the_declared_stream() => _stream.Stream.ShouldEqual("Ledger");
    [Fact] void should_match_the_disk_syntax() => SyntaxJson.StructurallyEqual(_memory.Result.Value!, _disk.Result.Value!).ShouldBeTrue();
    [Fact] void should_match_the_disk_diagnostics() => _memory.Result.Diagnostics.ShouldEqual(_disk.Result.Diagnostics);
    [Fact] void should_preserve_the_sources_in_disk_order() => _memory.Sources.Select(source => (source.File.RelativePath, source.Source)).ShouldEqual(_disk.Sources.Select(source => (source.File.RelativePath, source.Source)));
    [Fact] void should_not_invent_disk_paths() => _memory.Sources.Select(source => source.File.Path).ShouldEqual(_memory.Sources.Select(source => source.File.RelativePath));
}
