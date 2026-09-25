// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_paging_requirement_references_at_the_response_cap : given.a_connection
{
    JsonElement _page;

    void Because()
    {
        var source = new StringBuilder("module Projects\n  feature Registration\n    slice StateChange RegisterProject\n");
        for (var command = 0; command < 200; command++)
        {
            source.Append("      command Register").Append(command).Append('\n');
            for (var property = 0; property < 24; property++) source.Append("        property").Append(property).Append(" String\n");
            source.Append("        handler\n          file Handler").Append(command).Append(".cs\n");
        }
        File.WriteAllText(Path.Combine(RootPath, "application.play"), source.ToString());
        Initialize();
        var revision = Call("open-workspace", new { applicationName = "Projects" }).GetProperty("result").GetProperty("structuredContent").GetProperty("revision").GetString();
        _page = Call("read-workspace", new { expectedRevision = revision, view = "implementation-requirements", limit = 200 }).GetProperty("result").GetProperty("structuredContent");
    }

    [Fact] void should_return_all_references_under_the_response_cap()
    {
        _page.GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(200);
        Encoding.UTF8.GetByteCount(_page.GetRawText()).ShouldBeLessThan(McpJson.MaximumStructuredResponseBytes);
        _page.GetProperty("page").GetProperty("items")[0].GetProperty("typedContext").GetProperty("count").GetInt32().ShouldEqual(1);
    }
}
