// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_opening_the_configured_root_with_a_pending_worktree_journal : given.a_worktree_connection
{
    byte[] _marker;
    string _markerPath;
    JsonElement _opened;
    void Establish()
    {
        _ = Open(WorktreePath);
        var proposal = Rename(Workspace());
        _ = McpRecoveryJournal.Prepare(new McpRoot(WorktreeModelPath), proposal, new(null, McpState.Serialize(proposal.Workspace)));
        _markerPath = Path.Combine(WorktreeModelPath, ".screenplay", McpRecoveryJournal.FileName);
        _marker = File.ReadAllBytes(_markerPath);
    }
    void Because() => _opened = Open(ModelPath);
    [Fact] void should_open_the_configured_root_independently_of_worktree_recovery() => _opened.GetProperty("documentCount").GetInt32().ShouldEqual(1);
    [Fact] void should_leave_the_other_roots_pending_marker_untouched() => File.ReadAllBytes(_markerPath).ShouldEqual(_marker);
}
