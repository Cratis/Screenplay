// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Mcp;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Tool.for_ModelTest;

public class when_selecting_an_mcp_identity : Mcp.for_McpSpecificationExecution.given.a_model
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    void should_select_the_same_scenario_from_mcp_and_the_cli(bool persisted, bool singleFile)
    {
        if (persisted)
        {
            var root = new McpRoot(RootPath);
            var identity = ApplicationIdentity.Create("Durable identity");
            var documents = root.Read().Select(document => WorkspaceDocument.Create("durable-key", document.Path, document.Bytes.AsSpan())).ToArray();
            var workspace = ScreenplayWorkspace.Create(identity, "Renamed application", [.. documents], SemanticIdentityCatalog.Empty(identity));
            Directory.CreateDirectory(Path.Combine(RootPath, ".screenplay"));
            File.WriteAllBytes(Path.Combine(RootPath, ".screenplay", McpState.FileName), McpState.Serialize(workspace));
        }

        var report = Call("run-specifications", new { specification = "Projects.Registration.Register.Correct" }).GetProperty("result").GetProperty("structuredContent");
        var id = report.GetProperty("page").GetProperty("items")[0].GetProperty("semanticId").GetString()!;
        using var output = new StringWriter();
        using var error = new StringWriter();
        var target = singleFile ? Path.Combine(RootPath, "application.play") : RootPath;
        ModelTest.Run([target, "--filter", id, "--format", "json"], output, error).ShouldEqual(0);
        using var result = JsonDocument.Parse(output.ToString());
        result.RootElement.GetProperty("selected").GetInt32().ShouldEqual(1);
        result.RootElement.GetProperty("results")[0].GetProperty("semanticId").GetString().ShouldEqual(id);
        error.ToString().ShouldBeEmpty();
    }
}
