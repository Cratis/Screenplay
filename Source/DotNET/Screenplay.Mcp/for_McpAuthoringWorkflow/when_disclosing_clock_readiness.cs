// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_disclosing_clock_readiness : given.an_authoring_connection
{
    const string Prefix = "module M\n  feature F\n    slice StateChange S\n      command C\n        occurred DateTime\n";

    [Theory]
    [InlineData("time zone \"Europe/Oslo\"\n", "")]
    [InlineData("", "        calendar \"Europe/Oslo\"\n")]
    [InlineData("", "        produces when occurred < now - 2 hours\n          Recorded\n")]
    [InlineData("", "        produces when date of occurred < date of now\n          Recorded\n")]
    [InlineData("", "      specification T\n        given time zone \"Europe/Oslo\"\n")]
    void should_report_admitted_forms_as_executable(string root, string body)
    {
        var revision = Start(root + Prefix + body);
        var compilation = new ScreenplayCompiler().Compile(File.ReadAllText(Path.Combine(RootPath, "application.play")));
        compilation.Diagnostics.ShouldBeEmpty();
        Workspace().Compilation.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax && diagnostic.Message.Contains("0106", StringComparison.Ordinal)).ShouldBeFalse();
        var readiness = new McpAuthoringReadiness(compilation.Value!);
        readiness.ModelSyntaxOnly.ShouldBeFalse();
        readiness.ModelExecutionReadiness.ShouldBeNull();
        var inventory = Result("read-workspace", new { expectedRevision = revision, view = "event-sources" });
        inventory.GetProperty("executionReadiness").ValueKind.ShouldEqual(JsonValueKind.Null);
    }

    [Fact]
    void should_disclose_clock_operands_outside_command_rules()
    {
        var revision = Start(Prefix + "      screen Details\n        action \"Schedule\"\n          when date of item.occurred < date of now + 2 hours execute C\n");
        var readiness = new McpAuthoringReadiness(new ScreenplayCompiler().Parse(File.ReadAllText(Path.Combine(RootPath, "application.play"))).Value!);
        readiness.ModelSyntaxOnly.ShouldBeTrue();
        readiness.ModelExecutionReadiness!.ShouldContain("decisions 0106 and 0107");
        var inventory = Result("read-workspace", new { expectedRevision = revision, view = "event-sources" });
        inventory.GetProperty("executionReadiness").GetString().ShouldContain("decisions 0106 and 0107");
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
