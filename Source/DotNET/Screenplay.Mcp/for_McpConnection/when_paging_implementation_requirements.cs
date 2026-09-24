// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_paging_implementation_requirements : given.a_connection
{
    JsonElement _first;
    JsonElement _second;
    JsonElement _stale;

    void Because()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Source.Replace("        produces ProjectRegistered\n          for projectId\n          projectId = projectId\n          name = name", "        handler\n          file Handlers/RegisterProject.cs\n        validate csharp\n          ```\n          return true;\n          ```", StringComparison.Ordinal));
        Initialize();
        var opened = Call("open-workspace", new { applicationName = "Projects" });
        if (!opened.TryGetProperty("result", out var response)) throw new McpFailure(opened.GetRawText());
        var content = response.GetProperty("structuredContent");
        var revision = content.GetProperty("revision").GetString();
        _first = Call("read-workspace", new { expectedRevision = revision, view = "implementation-requirements", limit = 1 }).GetProperty("result").GetProperty("structuredContent").GetProperty("page");
        _second = Call("read-workspace", new { expectedRevision = revision, view = "implementation-requirements", offset = 1, limit = 1 }).GetProperty("result").GetProperty("structuredContent").GetProperty("page");
        _stale = Call("read-workspace", new { expectedRevision = "stale", view = "implementation-requirements" }).GetProperty("result");
    }

    [Fact] void should_page_one_requirement_at_a_time() => _first.GetProperty("items").GetArrayLength().ShouldEqual(1);
    [Fact] void should_list_the_handler_role() => _first.GetProperty("items")[0].GetProperty("role").GetString().ShouldEqual("CommandHandler");
    [Fact] void should_list_the_authored_file() => _first.GetProperty("items")[0].GetProperty("file").GetString().ShouldEqual("Handlers/RegisterProject.cs");
    [Fact] void should_page_the_validation_role() => _second.GetProperty("items")[0].GetProperty("role").GetString().ShouldEqual("CommandValidation");
    [Fact] void should_return_the_owner_address() => _first.GetProperty("items")[0].GetProperty("owner").ValueKind.ShouldEqual(JsonValueKind.Object);
    [Fact] void should_reject_stale_revisions() => _stale.GetProperty("isError").GetBoolean().ShouldBeTrue();
}
