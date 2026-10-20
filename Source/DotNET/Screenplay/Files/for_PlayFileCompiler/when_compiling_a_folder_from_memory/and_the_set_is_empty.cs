// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFileCompiler.when_compiling_a_folder_from_memory;

public class and_the_set_is_empty : given.documents_in_memory
{
    ApplicationCompilation<ApplicationSyntax> _compilation;

    void Because() => _compilation = _compiler.CompileFolder([]);

    [Fact] void should_succeed_like_an_empty_disk_folder() => _compilation.Result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _compilation.Result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_have_no_sources() => _compilation.Sources.ShouldBeEmpty();
}
