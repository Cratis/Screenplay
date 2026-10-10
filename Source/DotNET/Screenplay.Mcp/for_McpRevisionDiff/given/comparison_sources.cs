// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpRevisionDiff.given;

public class comparison_sources : for_McpConnection.given.a_connection
{
    internal JsonElement Opened;
    internal string Export = null!;

    void Establish()
    {
        Connection = new(new McpTools { CurrentDirectoryHint = RootPath });
        Initialize();
        Opened = Call("open-workspace", new { path = RootPath, includeContent = true }).GetProperty("result").GetProperty("structuredContent");
        Export = Opened.GetProperty("workspaceJson").GetString()!;
    }

    internal JsonElement Compare(object arguments) => Call("semantic-diff", arguments).GetProperty("result");
}
