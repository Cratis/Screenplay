// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_reviewing_semantic_diff_against_durable_state;

public class and_identity_state_changed : given.a_retained_proposal
{
    JsonElement _firstAfterDrift;
    JsonElement _continuation;

    void Establish() => File.AppendAllText(Files.PathFor(McpState.FileName), "\n");

    void Because()
    {
        _firstAfterDrift = ReadFirst();
        _continuation = Continue();
    }

    [Fact] void should_allow_review_against_the_original_identity_state() => First.GetProperty("isError").GetBoolean().ShouldBeFalse();
    [Fact] void should_refuse_the_changed_identity_baseline() => _firstAfterDrift.GetProperty("isError").GetBoolean().ShouldBeTrue();
    [Fact] void should_report_identity_state_drift() => _firstAfterDrift.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("IdentityStateDrift");
    [Fact] void should_refuse_a_revision_pinned_continuation_after_identity_drift() => _continuation.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("IdentityStateDrift");
    [Fact] void should_leave_play_files_unchanged() => File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(Source);
}
