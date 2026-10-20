// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Files.for_PlayFileCompiler.when_compiling_a_folder_from_memory;

public class and_paths_need_normalization : given.documents_in_memory
{
    ApplicationCompilation<ApplicationSyntax> _normalized;
    ApplicationCompilation<ApplicationSyntax> _original;

    void Because()
    {
        _original = _compiler.CompileFolder(_documents);
        _normalized = _compiler.CompileFolder(
        [
            new("./commands\\record.play", Command),
            new("nested/../z-sources.play", EventSource)
        ]);
    }

    [Fact] void should_succeed() => _normalized.Result.Success.ShouldBeTrue();
    [Fact] void should_use_normalized_relative_paths() => _normalized.Sources.Select(source => source.File.RelativePath).ShouldEqual(["commands/record.play", "z-sources.play"]);
    [Fact] void should_match_the_original_syntax_regardless_of_input_order() => SyntaxJson.StructurallyEqual(_normalized.Result.Value!, _original.Result.Value!).ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _normalized.Result.Diagnostics.ShouldBeEmpty();
}
