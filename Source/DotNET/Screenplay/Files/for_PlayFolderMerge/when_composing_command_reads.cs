// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFolderMerge;

public class when_composing_command_reads : Specification
{
    [Fact]
    void should_inherit_the_root_mode_across_imports()
    {
        var sources = new InMemoryPlayDocumentSource(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["application.play"] = "command reads protected\nimport \"module.play\"\n",
            ["module.play"] = "module M\n  feature F\n    slice StateChange S\n      command Submit\n        value String\n"
        });
        var (_, compilation) = PlayApplicationAssembly.Compile(new ScreenplayCompiler(), ["application.play"], sources);
        compilation.Success.ShouldBeTrue();
        compilation.Value!.CommandReadMode.ShouldEqual(CommandReadMode.Protected);
        var printed = new ScreenplayPrinter().Print(compilation.Value with { FileImports = [] });
        new ScreenplayCompiler().Parse(printed).Value!.CommandReadMode.ShouldEqual(CommandReadMode.Protected);
    }

    [Theory]
    [InlineData("command reads protected")]
    [InlineData("command reads legacy")]
    public void should_refuse_duplicate_or_conflicting_modes_across_files(string second)
    {
        var compiler = new ScreenplayCompiler();
        var merged = PlayFolderMerge.Merge([compiler.Parse("command reads protected", "application.play"), compiler.Parse(second, "other.play")]);
        merged.Success.ShouldBeFalse();
        merged.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidCommandReadsDeclaration && diagnostic.Location.Path == "other.play");
    }

    [Fact]
    void should_refuse_a_preamble_imported_into_a_module()
    {
        var sources = new InMemoryPlayDocumentSource(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["application.play"] = "module M\n  import \"fragment.play\"\n",
            ["fragment.play"] = "command reads protected\n"
        });
        var (_, compilation) = PlayApplicationAssembly.Compile(new ScreenplayCompiler(), ["application.play"], sources);
        compilation.Success.ShouldBeFalse();
        compilation.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidCommandReadsDeclaration && diagnostic.Location.Path == "fragment.play");
    }
}
