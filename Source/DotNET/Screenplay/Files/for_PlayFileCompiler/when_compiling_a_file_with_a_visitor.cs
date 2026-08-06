// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFileCompiler;

public class when_compiling_a_file_with_a_visitor : Specification
{
    const string Source =
        """
        module Invoicing
          feature Invoices
            slice StateChange Register
              event InvoiceRegistered
        """;

    IPlayFiles _playFiles;
    PlayFileCompiler _compiler;
    ApplicationCompilation<string> _compilation;

    void Establish()
    {
        _playFiles = Substitute.For<IPlayFiles>();
        _playFiles.ReadContent(Arg.Any<PlayFile>()).Returns(Source);
        _compiler = new(_playFiles, new ScreenplayCompiler());
    }

    void Because() => _compilation = _compiler.CompileFile(Path.Combine("some", "where", "invoicing.play"), new module_names());

    [Fact] void should_not_discover_any_files() => _playFiles.DidNotReceive().FindIn(Arg.Any<string>());
    [Fact] void should_succeed() => _compilation.Result.Success.ShouldBeTrue();
    [Fact] void should_drive_the_visitor() => _compilation.Result.Value.ShouldEqual("Invoicing");
    [Fact] void should_carry_the_source() => _compilation.Sources.Single().Source.ShouldEqual(Source);
    [Fact] void should_use_the_file_name_as_the_relative_path() => _compilation.Sources.Single().File.RelativePath.ShouldEqual("invoicing.play");

    class module_names : IApplicationSyntaxVisitor<string>
    {
        public string Visit(ApplicationSyntax syntax) => string.Join(", ", syntax.Modules.Select(module => module.Name));
    }
}
