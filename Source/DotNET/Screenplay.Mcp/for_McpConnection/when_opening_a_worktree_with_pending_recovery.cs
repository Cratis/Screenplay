// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_opening_a_worktree_with_pending_recovery : given.a_worktree_connection
{
    byte[] _marker;
    string _markerPath;
    JsonElement _refused;
    void Establish()
    {
        var proposal = Rename(Workspace());
        _ = McpRecoveryJournal.Prepare(new McpRoot(WorktreeModelPath), proposal, new(null, McpState.Serialize(proposal.Workspace)));
        _markerPath = Path.Combine(WorktreeModelPath, ".screenplay", McpRecoveryJournal.FileName);
        _marker = File.ReadAllBytes(_markerPath);
        _ = Open(ModelPath);
    }
    void Because() => _refused = Call("open-workspace", new { path = WorktreePath }).GetProperty("result");
    [Fact] void should_refuse_to_open_the_pending_worktree() => _refused.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("PendingOperation");
    [Fact] void should_preserve_the_worktree_journal() => File.ReadAllBytes(_markerPath).ShouldEqual(_marker);
}
