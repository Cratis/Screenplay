// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Files;

namespace Cratis.Screenplay.for_PlayFileCompiler;

public class when_compiling_a_single_file : given.a_play_file_compiler
{
    const string Source =
        """
        module Invoicing
          feature InvoiceManagement
            slice StateChange RegisterInvoice
              command RegisterInvoice
                invoiceId Uuid
        """;

    PlayFileCompilation _compilation;

    void Establish() => _playFiles.ReadContent(Arg.Any<PlayFile>()).Returns(Source);

    void Because() => _compilation = _compiler.CompileFile(Path.Combine("some", "where", "invoicing.play"));

    [Fact] void should_not_discover_any_files() => _playFiles.DidNotReceive().FindIn(Arg.Any<string>());
    [Fact] void should_compile_the_file() => _compilation.Result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _compilation.Result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_the_source() => _compilation.Source.ShouldEqual(Source);
    [Fact] void should_use_the_file_name_as_the_relative_path() => _compilation.File.RelativePath.ShouldEqual("invoicing.play");
    [Fact] void should_resolve_the_full_path() => Path.IsPathFullyQualified(_compilation.File.Path).ShouldBeTrue();
}
