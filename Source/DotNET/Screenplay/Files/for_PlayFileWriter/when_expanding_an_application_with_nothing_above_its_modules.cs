// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFileWriter;

public class when_expanding_an_application_with_nothing_above_its_modules : Specification
{
    const string Source =
        """
        module Invoicing
          feature Invoices
            slice StateChange Register
              event InvoiceRegistered
        """;

    DirectoryInfo _root;
    IEnumerable<PlayFileContent> _files;
    ApplicationCompilation<ApplicationSyntax> _recompiled;

    void Establish() => _root = Directory.CreateTempSubdirectory("playempty");

    void Because()
    {
        var application = new ScreenplayCompiler().Compile(Source).Value!;
        var writer = new PlayFileWriter();
        _files = writer.Expand(application);
        writer.WriteTo(application, _root.FullName);
        _recompiled = new PlayFileCompiler().CompileFolder(_root.FullName);
    }

    [Fact] void should_still_write_the_root_file() => _files.Select(file => file.RelativePath).ShouldContain(PlayFileWriter.RootFileName);
    [Fact] void should_leave_the_root_file_empty() => _files.Single(file => file.RelativePath == PlayFileWriter.RootFileName).Content.Trim().ShouldBeEmpty();
    [Fact] void should_compile_the_folder_back() => _recompiled.Result.Success.ShouldBeTrue();
    [Fact] void should_not_report_anything() => _recompiled.Result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_give_back_the_slice() => _recompiled.Result.Value!.Modules.Single().Features.Single().Slices.Single().Name.ShouldEqual("Register");

    void Destroy() => _root.Delete(true);
}
