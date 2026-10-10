// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpRevisionDiff;

public class when_comparing_sources_with_pending_recovery : given.comparison_sources
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
        _active = Compare(new { before = new { workspace = "active" }, afterWorkspaceJson = Export });
        _path = Compare(new { beforeWorkspaceJson = Export, after = new { path = RootPath } });
    }

    [Fact] void should_refuse_a_pending_active_workspace() => _active.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("PendingOperation");
    [Fact] void should_refuse_a_pending_path_source() => _path.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("PendingOperation");
    [Fact] void should_preserve_the_journal() => File.ReadAllText(Path.Combine(RootPath, ".screenplay", "pending.json")).ShouldEqual("pending");
}
