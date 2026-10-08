// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_reviewing_semantic_diff_against_durable_state;

public class and_recovery_is_pending : given.a_retained_proposal
{
    JsonElement _firstWithPendingRecovery;
    JsonElement _continuation;
    byte[] _marker = [];

    void Establish()
    {
        var interrupted = Rename(Original);
        McpRecoveryJournal.Prepare(Root, interrupted, new(BeforeState, McpState.Serialize(interrupted.Workspace)));
        _marker = Files.Read(McpRecoveryJournal.FileName)!;
    }

    void Because()
    {
        _firstWithPendingRecovery = ReadFirst();
        _continuation = Continue();
    }

    [Fact] void should_refuse_review_while_recovery_is_pending() => _firstWithPendingRecovery.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("PendingOperation");
    [Fact] void should_refuse_a_pinned_continuation_while_recovery_is_pending() => _continuation.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("PendingOperation");
    [Fact] void should_not_recover_implicitly() => Files.Read(McpRecoveryJournal.FileName).ShouldEqual(_marker);
    [Fact] void should_leave_play_files_unchanged() => File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(Source);
}
