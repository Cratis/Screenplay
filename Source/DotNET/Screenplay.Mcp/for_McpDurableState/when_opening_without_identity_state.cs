// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpDurableState;

public class when_opening_without_identity_state : for_McpConnection.given.a_connection
{
    JsonElement _opened;

    void Because() => _opened = JsonSerializer.SerializeToElement(new McpWorkspaces(Root).Open(McpJson.Empty));

    [Fact] void should_allow_a_first_open() => _opened.GetProperty("isError").GetBoolean().ShouldBeFalse();
    [Fact] void should_not_write_metadata_during_open() => Directory.Exists(Path.Combine(RootPath, ".screenplay")).ShouldBeFalse();
}
