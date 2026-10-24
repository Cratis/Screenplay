// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_disclosing_clock_readiness : given.an_authoring_connection
{
    const string Prefix = "module M\n  feature F\n    slice StateChange S\n      command C\n        occurred DateTime\n";

    [Theory]
    [InlineData("time zone \"Europe/Oslo\"\n", "", "Application")]
    [InlineData("", "        calendar \"Europe/Oslo\"\n", "Command")]
    [InlineData("", "        produces when occurred < now - 2 hours\n          Recorded\n", "Command")]
    [InlineData("", "        produces when date of occurred < date of now\n          Recorded\n", "Command")]
    [InlineData("", "      specification T\n        given time zone \"Europe/Oslo\"\n", "Specification")]
    void should_disclose_isolated_preview_forms_in_authoring_and_refuse_execution(string root, string body, string kind)
    {
        var revision = Start(root + Prefix + body);
        var compilation = new ScreenplayCompiler().Compile(File.ReadAllText(Path.Combine(RootPath, "application.play")));
        compilation.Diagnostics.ShouldBeEmpty();
        Workspace().Compilation.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax && diagnostic.Message.Contains("(#824)", StringComparison.Ordinal)).ShouldBeTrue();
        var readiness = new McpAuthoringReadiness(compilation.Value!);
        readiness.ModelSyntaxOnly.ShouldBeTrue();
        readiness.ModelExecutionReadiness!.ShouldContain("(#824)");
        var inventory = Result("read-workspace", new { expectedRevision = revision, view = "event-sources" });
        inventory.GetProperty("executionReadiness").GetString().ShouldContain("(#824)");
        if (kind == "Application") return;
        var address = kind == "Command" ? "M.F.S.C" : "M.F.S.T";
        var details = Result("declaration-details", new { address, kind, view = "summary" }).GetProperty("details");
        details.GetProperty("syntaxOnly").GetBoolean().ShouldBeTrue();
        details.GetProperty("executionReadiness").GetString().ShouldContain("(#824)");
        var unrelated = Result("declaration-details", new { address = "M.F.Other.Unrelated", kind = "Command", view = "summary" }).GetProperty("details");
        unrelated.GetProperty("syntaxOnly").GetBoolean().ShouldBeFalse();
        unrelated.GetProperty("executionReadiness").ValueKind.ShouldEqual(JsonValueKind.Null);
    }

    [Fact]
    void should_preserve_bare_today_and_now_as_properties()
    {
        Start(Prefix + "        today DateTime\n        now DateTime\n        produces when now < today\n          Recorded\n");
        var readiness = new McpAuthoringReadiness(new ScreenplayCompiler().Parse(File.ReadAllText(Path.Combine(RootPath, "application.play"))).Value!);
        readiness.ModelSyntaxOnly.ShouldBeFalse();
        readiness.ModelExecutionReadiness.ShouldBeNull();
    }

    string Start(string source)
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), source + "      event Recorded\n    slice StateChange Other\n      command Unrelated\n");
        Initialize();
        return Open().GetProperty("revision").GetString()!;
    }
}
