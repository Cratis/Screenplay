// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Parsing;

namespace Cratis.Screenplay.for_Agents;

public class when_composing_agent_files : given.an_agent_model
{
    [Fact]
    void should_resolve_references_after_merging()
    {
        var result = PlayFolderMerge.Merge([Compiler.Parse(Allowlist, "agents.play"), Compiler.Parse(Model, "sales.play")]);
        var context = ParserContext.ForDiagnostics();
        ScreenplayValidator.Validate(result.Value!, context);
        result.Success.ShouldBeTrue();
        context.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        result.Value!.Agents!.Location.Path.ShouldEqual("agents.play");
    }

    [Fact]
    void should_reject_a_second_folder_block()
    {
        var result = PlayFolderMerge.Merge([Compiler.Parse(Allowlist, "agents.play"), Compiler.Parse(Allowlist, "other.play"), Compiler.Parse(Model, "sales.play")]);
        result.Success.ShouldBeFalse();
        result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(DiagnosticCodes.DuplicateAgentsDeclaration);
    }

    [Fact]
    void should_not_lose_the_block_when_expanding_files()
    {
        var result = Compiler.Compile(Model + "\n" + Allowlist);
        var files = new PlayFileWriter().Expand(result.Value!);
        files.Single(file => file.RelativePath == "application.play").Content.ShouldContain("tokens personal");
    }
}
