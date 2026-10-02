// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_showing_views.given;

public class a_host_that_renders_views : for_McpConnection.given.a_connection
{
    internal const string BoardHtml = "<!DOCTYPE html><html><body>board</body></html>";

    internal const string RenamedSlice = """

            slice StateChange RenameProject
              command RenameProject
                projectId ProjectId identifier
                name ProjectName
                produces ProjectRenamed
                  for projectId
                  projectId = projectId
                  name = name
              event ProjectRenamed
                projectId ProjectId
                name ProjectName
        """;

    internal JsonElement Initialized;

    void Establish()
    {
        Connection = new(new McpTools(Root), new McpAppResources(BoardHtml));
        Initialized = Handle(/*lang=json,strict*/ """{"jsonrpc":"2.0","id":0,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{"extensions":{"io.modelcontextprotocol/ui":{"mimeTypes":["text/html;profile=mcp-app"]}}},"clientInfo":{"name":"host","version":"1"}}}""");
        Connection.Handle(/*lang=json,strict*/ """{"jsonrpc":"2.0","method":"notifications/initialized"}""");
    }

    internal JsonElement Handle(string request)
    {
        using var result = JsonDocument.Parse(Connection.Handle(request)!);
        return result.RootElement.Clone();
    }
}
