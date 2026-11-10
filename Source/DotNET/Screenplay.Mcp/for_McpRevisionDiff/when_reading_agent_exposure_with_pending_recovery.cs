// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpRevisionDiff;

public class when_reading_agent_exposure_with_pending_recovery : given.comparison_sources
{
    JsonElement _active;
    JsonElement _path;

    void Establish()
    {
        McpFileAccess.CreatePrivateDirectory(Path.Combine(RootPath, ".screenplay"));
        using var journal = McpFileAccess.CreatePrivate(Path.Combine(RootPath, ".screenplay", "pending.json"));
        journal.Write("pending"u8);
    }

    void Because()
    {
        _active = Compare(new { before = new { workspace = "active" }, afterWorkspaceJson = Export, view = "agent-exposure", version = 2 });
        _path = Compare(new { beforeWorkspaceJson = Export, after = new { path = RootPath }, view = "agent-exposure", version = 2 });
    }

    [Fact] void should_preserve_the_active_workspace_recovery_guard() => _active.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("PendingOperation");
    [Fact] void should_preserve_the_path_recovery_guard() => _path.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("PendingOperation");
}
