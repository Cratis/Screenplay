// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFileCompiler.when_compiling_an_application_from_memory;

public class and_an_import_is_missing : when_compiling_a_folder_from_memory.given.documents_in_memory
{
    ApplicationCompilation<ApplicationSyntax> _memory;
    ApplicationCompilation<ApplicationSyntax> _disk;

    void Establish() => _documents = [new("apps/application.play", "import \"missing.play\"")];

    void Because()
    {
        _memory = _compiler.CompileApplication("apps/application.play", _documents);
        _disk = FromDisk("apps/application.play");
    }

    [Fact] void should_fail() => _memory.Result.Success.ShouldBeFalse();
    [Fact] void should_match_disk_diagnostics() => _memory.Result.Diagnostics.ShouldEqual(_disk.Result.Diagnostics);
    [Fact] void should_locate_the_error_relative_to_the_entry_folder() => _memory.Result.Diagnostics.Single().Location.Path.ShouldEqual("application.play");
}
