// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpRevisionDiff;

public class when_comparing_the_active_workspace_with_persisted_identities : given.comparison_sources
{
    JsonElement _result;

    void Establish()
    {
        McpFileAccess.CreatePrivateDirectory(Path.Combine(RootPath, ".screenplay"));
        using (var state = McpFileAccess.CreatePrivate(Path.Combine(RootPath, ".screenplay", "identities.json")))
        {
            state.Write(McpState.Serialize(McpWorkspaceTransport.Restore(Export)));
        }
        Call("open-workspace", new { path = RootPath });
    }

    void Because() => _result = Compare(new { before = new { workspace = "active" }, after = new { workspaceJson = Export } });

    [Fact] void should_admit_the_persisted_workspace() => _result.GetProperty("isError").GetBoolean().ShouldBeFalse();
    [Fact] void should_keep_identity_matching() => _result.GetProperty("structuredContent").GetProperty("limits").GetArrayLength().ShouldEqual(4);
    [Fact] void should_find_no_changes() => _result.GetProperty("structuredContent").GetProperty("page").GetProperty("items").EnumerateArray().ShouldBeEmpty();
}
