// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_disclosing_operation_authoring_readiness : given.an_authoring_connection
{
    const string OperationSource = "system Mailer\nmodule Projects\n  feature F\n    slice StateChange S\n      operation Send\n        uses Mailer\n      command C\n        produces event Recorded\n        produces Send\n      specification T\n        when C\n        then operation Send\n";

    [Fact]
    void should_disclose_unadmitted_operations_for_commands_specifications_slices_and_systems()
    {
        Start(OperationSource);
        var revision = Open().GetProperty("revision").GetString();
        foreach (var view in new[] { "system-intents", "operation-intents" })
        {
            var inventory = Result("read-workspace", new { expectedRevision = revision, view });
            inventory.GetProperty("executionReadiness").GetString().ShouldContain("(#301)");
            inventory.GetProperty("page").GetProperty("items")[0].GetProperty("executionReadiness").GetString().ShouldContain("(#301)");
        }
        foreach (var (address, kind) in new[] { ("Projects.F.S.C", "Command"), ("Projects.F.S.T", "Specification"), ("Projects.F.S", "Slice"), ("Mailer", "System"), ("Projects.F.S.Send", "Operation") })
        {
            var details = Result("declaration-details", new { address, kind, view = "summary" }).GetProperty("details");
            details.GetProperty("syntaxOnly").GetBoolean().ShouldBeTrue();
            details.GetProperty("executionReadiness").GetString().ShouldContain("operations and systems (#301)");
        }
        var commands = Result("declaration-details", new { address = "Projects.F.S", kind = "Slice", view = "commands" }).GetProperty("details").GetProperty("items");
        commands[0].GetProperty("producedEvents").EnumerateArray().Select(item => item.GetString()).ShouldEqual("Recorded");
        commands[0].GetProperty("executionReadiness").GetString().ShouldContain("operations and systems (#301)");
        var specifications = Result("declaration-details", new { address = "Projects.F.S", kind = "Slice", view = "specifications" }).GetProperty("details").GetProperty("items");
        specifications[0].GetProperty("syntaxOnly").GetBoolean().ShouldBeTrue();
        var indexed = Result("find-declaration", new { name = "C", kind = "Command", includeContent = true }).GetRawText();
        indexed.ShouldContain("operations and systems (#301)");
        indexed.ShouldContain("\"produces\":[\"Recorded\"]");
    }

    [Fact]
    void should_disclose_unavailable_operation_actions_even_without_operation_assertions()
    {
        Start(OperationSource.Replace("        then operation Send\n", "        then Recorded\n", StringComparison.Ordinal));
        var details = Result("declaration-details", new { address = "Projects.F.S.T", kind = "Specification", view = "summary" }).GetProperty("details");
        details.GetProperty("syntaxOnly").GetBoolean().ShouldBeTrue();
        details.GetProperty("executionReadiness").GetString().ShouldContain("operations and systems (#301)");
    }

    [Fact]
    void should_keep_responses_admitted_without_hiding_unadmitted_operations()
    {
        Start(OperationSource.Replace("        produces Send\n", "        produces Send\n        name String\n        returns name\n", StringComparison.Ordinal));
        Response().GetProperty("executionReadiness").GetString().ShouldContain("operations and systems (#301)");
        Response().GetProperty("executionReadiness").GetString().ShouldNotContain("(#300/#303)");
        Start("module Projects\n  feature F\n    slice StateChange S\n      command C\n        name String\n        returns name\n");
        Response().GetProperty("executionReadiness").ValueKind.ShouldEqual(JsonValueKind.Null);
        Response().GetProperty("syntaxOnly").GetBoolean().ShouldBeFalse();
    }

    void Start(string source)
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), source);
        Initialize();
        Open();
    }

    JsonElement Response() => Result("declaration-details", new { address = "Projects.F.S.C", kind = "Command", view = "response" }).GetProperty("details");
}
